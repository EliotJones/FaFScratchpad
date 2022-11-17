namespace FaFPlayground.Tests;

public class CompressedLabelTreeCompressTests
{
    private const bool t = true;
    private const bool f = false;

    [Theory]
    [InlineData(2)]
    [InlineData(1)]
    public void TwoByTwoIsFine(int compressionThreshold)
    {
        var treeRoot = GivenTreeWithCompressionLevel(2);

        var cache = new NavLabelCache(
            new List<IReadOnlyList<bool>>
            {
                new[] { t, t },
                new[] { t, t }
            });

        treeRoot.Compress(cache, compressionThreshold);

        Assert.True(treeRoot.Label);
        Assert.Empty(treeRoot.Children);
        Assert.Equal(1, Globals.NavLayerData[NavLayers.Air].PathableLeafs);
        Assert.Equal(0, Globals.NavLayerData[NavLayers.Air].UnpathableLeafs);
    }

    [Fact]
    public void TwoByTwoIsNotFineButNoFurtherCompression()
    {
        var treeRoot = GivenTreeWithCompressionLevel(2);

        var cache = new NavLabelCache(
            new List<IReadOnlyList<bool>>
            {
                new[] { t, t },
                new[] { t, f }
            });

        treeRoot.Compress(cache, 2);

        Assert.False(treeRoot.Label);
        Assert.Equal(0, Globals.NavLayerData[NavLayers.Air].PathableLeafs);
        Assert.Equal(1, Globals.NavLayerData[NavLayers.Air].UnpathableLeafs);
    }

    [Theory]
    [InlineData(1, 1, 3)]
    [InlineData(1, 0, 2)]
    [InlineData(0, 1, 1)]
    [InlineData(0, 0, 0)]
    public void TwoByTwoIsNotFineFurtherCompression(int row, int col, int index)
    {
        var treeRoot = GivenTreeWithCompressionLevel(2);

        var navData = new List<bool[]>
        {
            new[] { t, t },
            new[] { t, t }
        };

        navData[row][col] = false;

        var cache = new NavLabelCache(navData);

        treeRoot.Compress(cache, 1);

        Assert.Null(treeRoot.Label);
        Assert.Equal(1, Globals.NavLayerData[NavLayers.Air].Subdivisions);
        Assert.Equal(3, Globals.NavLayerData[NavLayers.Air].PathableLeafs);
        Assert.Equal(1, Globals.NavLayerData[NavLayers.Air].UnpathableLeafs);
        Assert.Equal(4, treeRoot.Children.Count);

        for (int i = 0; i < treeRoot.Children.Count; i++)
        {
            var child = treeRoot.Children[i];

            if (i == index)
            {
                Assert.False(child.Label);
                Assert.Empty(child.Children);
            }
            else
            {
                Assert.True(child.Label);
                Assert.Empty(child.Children);
            }
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void FourByFourFine(int compressionLevel)
    {
        var treeRoot = GivenTreeWithCompressionLevel(4);

        var cache = new NavLabelCache(
            new List<IReadOnlyList<bool>>
            {
                new[] { t, t, t, t },
                new[] { t, t, t, t },
                new[] { t, t, t, t },
                new[] { t, t, t, t }
            });

        treeRoot.Compress(cache, compressionLevel);

        Assert.True(treeRoot.Label);
        Assert.Equal(1, Globals.NavLayerData[NavLayers.Air].PathableLeafs);
        Assert.Equal(0, Globals.NavLayerData[NavLayers.Air].UnpathableLeafs);
    }

    [Fact]
    public void FourByFourSingleNotFineCompressionLevel2()
    {
        var treeRoot = GivenTreeWithCompressionLevel(4);

        var cache = new NavLabelCache(
            new List<IReadOnlyList<bool>>
            {
                new[] { t, t, t, t },
                new[] { t, t, f, t },
                new[] { t, t, t, t },
                new[] { t, t, t, t }
            });

        treeRoot.Compress(cache, 2);

        Assert.Null(treeRoot.Label);
        Assert.Equal(1, Globals.NavLayerData[NavLayers.Air].Subdivisions);
        Assert.Equal(3, Globals.NavLayerData[NavLayers.Air].PathableLeafs);
        Assert.Equal(1, Globals.NavLayerData[NavLayers.Air].UnpathableLeafs);
        Assert.Equal(4, treeRoot.Children.Count);

        for (var i = 0; i < treeRoot.Children.Count; i++)
        {
            var child = treeRoot.Children[i];

            if (i == 1)
            {
                Assert.False(child.Label);
            }
            else
            {
                Assert.True(child.Label);
            }
        }
    }

    [Fact]
    public void FourByFourSingleNotFineCompressionLevel1()
    {
        var treeRoot = GivenTreeWithCompressionLevel(4);

        var cache = new NavLabelCache(
            new List<IReadOnlyList<bool>>
            {
                new[] { t, t, t, t },
                new[] { t, t, f, t },
                new[] { t, t, t, t },
                new[] { t, t, t, t }
            });

        /*
         * Divide into 7 blocks:
         * 1:
         * t t
         * t t
         *
         * 2 - 5:
         * t | t | f | t
         *
         * 6:
         * t t
         * t t
         *
         * 7:
         * t t
         * t t
         */

        treeRoot.Compress(cache, 1);

        Assert.Null(treeRoot.Label);
        Assert.Equal(2, Globals.NavLayerData[NavLayers.Air].Subdivisions);
        Assert.Equal(6, Globals.NavLayerData[NavLayers.Air].PathableLeafs);
        Assert.Equal(1, Globals.NavLayerData[NavLayers.Air].UnpathableLeafs);
        Assert.Equal(4, treeRoot.Children.Count);

        for (var i = 0; i < treeRoot.Children.Count; i++)
        {
            var child = treeRoot.Children[i];

            if (i == 1)
            {
                Assert.Null(child.Label);
                Assert.Equal(4, child.Children.Count);
                for (var j = 0; j < child.Children.Count; j++)
                {
                    var grandchild = child.Children[j];

                    if (j == 2)
                    {
                        Assert.False(grandchild.Label);
                    }
                    else
                    {
                        Assert.True(grandchild.Label);
                    }
                }
            }
            else
            {
                Assert.True(child.Label);
            }
        }
    }

    [Fact]
    public void FourByFourTwoNotFineCompressionLevel1()
    {
        var treeRoot = GivenTreeWithCompressionLevel(4);

        var cache = new NavLabelCache(
            new List<IReadOnlyList<bool>>
            {
                new[] { t, t, t, t },
                new[] { t, t, f, t },
                new[] { f, t, t, t },
                new[] { t, t, t, t }
            });

        /*
         * Divide into 7 blocks:
         * 1:
         * t t
         * t t
         *
         * 2 - 5:
         * t | t | f | t
         *
         * 6 - 9
         * f | t | t | t
         *
         * 10:
         * t t
         * t t
         */

        treeRoot.Compress(cache, 1);

        Assert.Null(treeRoot.Label);
        Assert.Equal(3, Globals.NavLayerData[NavLayers.Air].Subdivisions);
        Assert.Equal(8, Globals.NavLayerData[NavLayers.Air].PathableLeafs);
        Assert.Equal(2, Globals.NavLayerData[NavLayers.Air].UnpathableLeafs);
        Assert.Equal(4, treeRoot.Children.Count);

        for (var i = 0; i < treeRoot.Children.Count; i++)
        {
            var child = treeRoot.Children[i];

            if (i == 1)
            {
                Assert.Null(child.Label);
                Assert.Equal(4, child.Children.Count);
                for (var j = 0; j < child.Children.Count; j++)
                {
                    var grandchild = child.Children[j];

                    if (j == 2)
                    {
                        Assert.False(grandchild.Label);
                    }
                    else
                    {
                        Assert.True(grandchild.Label);
                    }
                }
            }
            else if (i == 2)
            {
                Assert.Null(child.Label);
                Assert.Equal(4, child.Children.Count);
                for (var j = 0; j < child.Children.Count; j++)
                {
                    var grandchild = child.Children[j];

                    if (j == 0)
                    {
                        Assert.False(grandchild.Label);
                    }
                    else
                    {
                        Assert.True(grandchild.Label);
                    }
                }
            }
            else
            {
                Assert.True(child.Label);
            }
        }
    }

    private static CompressedLabelTree GivenTreeWithCompressionLevel(int level)
    {
        Globals.NavLayerData = new NavLayerData();
        return new(NavLayers.Air, 0, 0, level, 0, 0);
    }
}