namespace FaFPlayground;

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

    private bool? _label;
    public bool? Label => _label;

    public CompressedLabelTree(NavLayers layer, int bx, int bz, int c, int ox, int oz)
    {
        _layer = layer;
        _c = c;
        _ox = ox;
        _oz = oz;
        _bx = bx;
        _bz = bz;
    }

    public void Compress(NavLabelCache rCache, int compressionThreshold)
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
                _label = value;

                if (_label == true)
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
                _label = false;
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
            _label = value;

            if (_label == true)
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