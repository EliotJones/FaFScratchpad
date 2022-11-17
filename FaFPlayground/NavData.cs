namespace FaFPlayground;

// Issue: https://github.com/FAForever/fa/issues/4314
// Reference code from: https://github.com/FAForever/fa/blob/deploy/fafdevelop/lua/sim/NavGenerator.lua#L281
public static class Globals
{
    public static NavLayerData NavLayerData = new NavLayerData();
}

public enum NavLayers
{
    Air,
    Land,
    Amphibious
}

public class NavLayerDataItem
{
    public int Subdivisions;
    public int PathableLeafs = 0;
    public int UnpathableLeafs = 0;
    public int Neighbors = 0;
    public int Labels = 0;
}

public class NavLayerData
{
    private readonly Dictionary<NavLayers, NavLayerDataItem> _layers = new Dictionary<NavLayers, NavLayerDataItem>();

    public NavLayerDataItem this[NavLayers layer]
    {
        get
        {
            if (!_layers.TryGetValue(layer, out var item))
            {
                item = new NavLayerDataItem();
                _layers.Add(layer, item);
            }

            return item;
        }
    }
}

public class NavLabelCache
{
    private readonly IReadOnlyList<IReadOnlyList<bool>> _labels;

    public NavLabelCache(
        IReadOnlyList<IReadOnlyList<bool>> labels)
    {
        _labels = labels;
    }

    public IReadOnlyList<bool> this[int z] => _labels[z];
}

public class CompressedLabelTree
{
    private readonly NavLayers _layer;
    private readonly int _c;
    private readonly int _ox;
    private readonly int _oz;
    private readonly int _bx;
    private readonly int _bz;

    public readonly List<CompressedLabelTree> Children = new List<CompressedLabelTree>();

    public bool? Label { get; private set; }

    public CompressedLabelTree(NavLayers layer, int bx, int bz, int c, int ox, int oz)
    {
        _layer = layer;
        _c = c;
        _ox = ox;
        _oz = oz;
        _bx = bx;
        _bz = bz;
    }

    public void Compress(
        NavLabelCache rCache,
        int compressionThreshold)
    {
        var result = CompressThirdly(
            rCache,
            _oz,
            _ox,
            _c,
            compressionThreshold,
            Children);

        if (Children.Count <= 1)
        {
            Children.Clear();

            if (result == true)
            {
                Globals.NavLayerData[_layer].PathableLeafs = Globals.NavLayerData[_layer].PathableLeafs + 1;

            }
            else
            {
                Globals.NavLayerData[_layer].UnpathableLeafs = Globals.NavLayerData[_layer].UnpathableLeafs + 1;
            }
        }

        Label = result;
    }

    private bool? CompressThirdly(
        NavLabelCache rCache,
        int offsetZ,
        int offsetX,
        int currentStep,
        int minStep,
        List<CompressedLabelTree> children)
    {
        var isBaseCase = currentStep == minStep;

        // Used to ensure all values in this quad share the same value.
        // If not we need to split further.
        var i = 0;
        var allShareValue = true;
        bool? result = true;

        // Used to keep counter of leaf nodes and subdivisions up-to-date.
        var pathable = 0;
        var unpathable = 0;

        var newStep = currentStep / 2;
        var increment = isBaseCase ? 1 : newStep;
        for (int z = offsetZ; z < offsetZ + currentStep; z += increment)
        {
            for (int x = offsetX; x < offsetX + currentStep; x += increment)
            {
                var myChildren = new List<CompressedLabelTree>();

                if (isBaseCase)
                {
                    result &= rCache[z][x];

                    if (result == false)
                    {
                        break;
                    }
                }
                else
                {
                    var inner = CompressThirdly(
                        rCache,
                        z,
                        x,
                        newStep,
                        minStep,
                        myChildren);

                    if (i == 0)
                    {
                        result = inner;
                    }
                    else if (inner != result)
                    {
                        allShareValue = false;
                    }

                    if (inner == true)
                    {
                        pathable++;
                    }
                    else if (inner == false)
                    {
                        unpathable++;
                    }
                }

                // If we have children then wrap them in a node
                if (myChildren.Count > 0)
                {
                    var child = new CompressedLabelTree(
                        _layer,
                        _bx,
                        _bz,
                        currentStep,
                        offsetX,
                        offsetZ);

                    child.Children.AddRange(myChildren);

                    children.Add(child);
                }

                i++;
            }

            if (isBaseCase && result == false)
            {
                break;
            }
        }

        if (isBaseCase)
        {
            children.Add(new CompressedLabelTree(
                _layer,
                _bx,
                _bz,
                currentStep,
                offsetX,
                offsetZ)
            {
                Label = result
            });

            return result;
        }

        if (allShareValue)
        {
            // All sub-quads are the same so we don't need to subdivide.
            return result;
        }

        Globals.NavLayerData[_layer].Subdivisions = Globals.NavLayerData[_layer].Subdivisions + 1;
        Globals.NavLayerData[_layer].PathableLeafs = Globals.NavLayerData[_layer].PathableLeafs + pathable;
        Globals.NavLayerData[_layer].UnpathableLeafs = Globals.NavLayerData[_layer].UnpathableLeafs + unpathable;

        return null;
    }

    private bool? CompressOther(
        NavLabelCache rCache,
        bool isRoot,
        int currentStep,
        int minStep,
        int offsetZ,
        int offsetX,
        List<CompressedLabelTree> children)
    {
        // Base case.
        if (currentStep == minStep)
        {
            var isLeafUniform = true;
            for (var z = offsetZ; z < offsetZ + currentStep; z++)
            {
                for (var x = offsetX; x < offsetX + currentStep; x++)
                {
                    isLeafUniform &= rCache[z][x];

                    if (!isLeafUniform)
                    {
                        if (!isRoot)
                        {
                            children.Add(new CompressedLabelTree(_layer, _bx, _bz, currentStep, offsetX, offsetZ)
                            {
                                Label = false
                            });
                        }

                        return false;
                    }
                }
            }

            if (!isRoot)
            {
                children.Add(new CompressedLabelTree(_layer, _bx, _bz, currentStep, offsetX, offsetZ)
                {
                    Label = true
                });
            }

            return true;
        }

        var allHaveSameResult = true;
        bool? result = true;
        var i = 0;

        var pathable = 0;
        var unpathable = 0;

        var newStep = currentStep / 2;
        var innerChildren = new List<CompressedLabelTree>();
        for (int z = offsetZ; z < offsetZ + currentStep; z += newStep)
        {
            for (int x = offsetX; x < offsetX + currentStep; x += newStep)
            {
                var child = CompressOther(
                    rCache,
                    false,
                    newStep,
                    minStep,
                    z,
                    x,
                    innerChildren);

                if (child == true)
                {
                    pathable++;
                }
                else if (child == false)
                {
                    unpathable++;
                }

                if (i > 0 && child != result)
                {
                    allHaveSameResult = false;
                }

                result = child;
                i++;
            }
        }

        // All children share the same value so the split is not necessary.
        if (allHaveSameResult)
        {
            return result;
        }

        Globals.NavLayerData[_layer].PathableLeafs = Globals.NavLayerData[_layer].PathableLeafs + pathable;
        Globals.NavLayerData[_layer].UnpathableLeafs = Globals.NavLayerData[_layer].UnpathableLeafs + unpathable;

        if (innerChildren.Count > 0)
        {
            Globals.NavLayerData[_layer].Subdivisions = Globals.NavLayerData[_layer].Subdivisions + 1;

            if (isRoot)
            {
                children.AddRange(innerChildren);
            }
            else
            {
                var newChild = new CompressedLabelTree(_layer, _bx, _bz, currentStep, offsetX, offsetZ);
                newChild.Children.AddRange(innerChildren);
                children.Add(newChild);
            }
        }

        return null;
    }

    private void IncrementResultCounter(bool? result)
    {
        if (result == true)
        {
            Globals.NavLayerData[_layer].PathableLeafs = Globals.NavLayerData[_layer].PathableLeafs + 1;
        }
        else if (result == false)
        {
            Globals.NavLayerData[_layer].UnpathableLeafs = Globals.NavLayerData[_layer].UnpathableLeafs + 1;
        }
    }

    public void CompressReference(NavLabelCache rCache, int compressionThreshold)
    {
        var uniform = true;
        var value = rCache[_oz][_ox];
        if (_c <= compressionThreshold)
        {
            for (int z = _oz; z < _oz + _c; z++)
            {
                for (int x = _ox; x < _ox + _c; x++)
                {
                    uniform &= rCache[z][x];

                    if (!uniform)
                    {
                        break;
                    }
                }

                if (!uniform)
                {
                    break;
                }
            }

            if (uniform)
            {
                Label = value;

                if (Label == true)
                {
                    Globals.NavLayerData[_layer].PathableLeafs = Globals.NavLayerData[_layer].PathableLeafs + 1;
                }
                else
                {
                    Globals.NavLayerData[_layer].UnpathableLeafs = Globals.NavLayerData[_layer].UnpathableLeafs + 1;
                }
            }
            else
            {
                Label = false;
                Globals.NavLayerData[_layer].UnpathableLeafs = Globals.NavLayerData[_layer].UnpathableLeafs + 1;
            }

            return;
        }

        // recursive case where we do make children

        value = rCache[_oz][_ox];
        uniform = true;
        for (int z = _oz; z < _oz + _c; z++)
        {
            for (int x = _ox; x < _ox + _c; x++)
            {
                uniform &= rCache[z][x];

                if (!uniform)
                {
                    break;
                }
            }

            if (!uniform)
            {
                break;
            }
        }

        if (uniform)
        {
            // we're uniform, so we're good
            Label = value;

            if (Label == true)
            {
                Globals.NavLayerData[_layer].PathableLeafs = Globals.NavLayerData[_layer].PathableLeafs + 1;
            }
            else
            {
                Globals.NavLayerData[_layer].UnpathableLeafs = Globals.NavLayerData[_layer].UnpathableLeafs + 1;
            }
        }
        else
        {
            // We're not uniform, split up to children
            var hc = (int)(0.5 * _c);

            Children.Add(new CompressedLabelTree(_layer, _bx, _bz, hc, _ox, _oz));
            Children.Add(new CompressedLabelTree(_layer, _bx, _bz, hc, _ox + hc, _oz));
            Children.Add(new CompressedLabelTree(_layer, _bx, _bz, hc, _ox, _oz + hc));
            Children.Add(new CompressedLabelTree(_layer, _bx, _bz, hc, _ox + hc, _oz + hc));

            foreach (var child in Children)
            {
                child.Compress(rCache, compressionThreshold);
            }

            Globals.NavLayerData[_layer].Subdivisions = Globals.NavLayerData[_layer].Subdivisions + 1;
        }
    }
}