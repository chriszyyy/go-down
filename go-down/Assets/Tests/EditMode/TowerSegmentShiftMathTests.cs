using NUnit.Framework;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 验证塔段累计随机游走、偏移上限和错位接缝支撑位置。
/// </summary>
[TestFixture]
public class TowerSegmentShiftMathTests
{
    [Test]
    public void AllowedMaxOffset_IncreasesGraduallyAndStopsAtConfiguredMaximum()
    {
        Assert.AreEqual(1, TowerSegmentShiftMath.CalculateAllowedMaxOffset(0, 1, 4, 3));
        Assert.AreEqual(1, TowerSegmentShiftMath.CalculateAllowedMaxOffset(3, 1, 4, 3));
        Assert.AreEqual(2, TowerSegmentShiftMath.CalculateAllowedMaxOffset(4, 1, 4, 3));
        Assert.AreEqual(3, TowerSegmentShiftMath.CalculateAllowedMaxOffset(20, 1, 4, 3));
    }

    [Test]
    public void NextOffset_AccumulatesOneCellPerSegment()
    {
        int offset = TowerSegmentShiftMath.GetNextOffset(0, 1, 3);
        offset = TowerSegmentShiftMath.GetNextOffset(offset, 1, 3);
        offset = TowerSegmentShiftMath.GetNextOffset(offset, 1, 3);

        Assert.AreEqual(3, offset);
    }

    [TestCase(3, 1, 3, -1)]
    [TestCase(-3, -1, 3, 1)]
    public void DirectionAtLimit_IsForcedBackTowardCenter(
        int currentOffset,
        int direction,
        int limit,
        int expectedDirection)
    {
        Assert.AreEqual(
            expectedDirection,
            TowerSegmentShiftMath.ResolveDirectionAtLimit(currentOffset, direction, limit));
    }

    [Test]
    public void Walk_CanMoveRightThenReverseAndCrossToLeft()
    {
        int offset = 0;
        for (int i = 0; i < 3; i++)
            offset = TowerSegmentShiftMath.GetNextOffset(offset, 1, 3);
        for (int i = 0; i < 5; i++)
            offset = TowerSegmentShiftMath.GetNextOffset(offset, -1, 3);

        Assert.AreEqual(-2, offset);
    }

    [Test]
    public void BridgeColumns_RightStep_AreContinuousAndCoverBothOuterEdges()
    {
        int[] columns = TowerSegmentShiftMath.GetBridgeColumns(2, 3, 5);

        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6, 7 }, columns);
    }

    [Test]
    public void BridgeColumns_LeftStep_AreContinuousAndCoverBothOuterEdges()
    {
        int[] columns = TowerSegmentShiftMath.GetBridgeColumns(-1, -2, 5);

        CollectionAssert.AreEqual(new[] { -2, -1, 0, 1, 2, 3 }, columns);
    }

    [TestCase(2, 3, 5)]
    [TestCase(-1, -2, 5)]
    public void BridgeColumns_DoNotExtendBeyondMinimalUnion(
        int previousOffset,
        int nextOffset,
        int layerWidth)
    {
        int[] columns = TowerSegmentShiftMath.GetBridgeColumns(
            previousOffset, nextOffset, layerWidth);
        int expectedMin = System.Math.Min(previousOffset, nextOffset);
        int expectedMax = System.Math.Max(
            previousOffset + layerWidth - 1,
            nextOffset + layerWidth - 1);

        Assert.AreEqual(expectedMin, columns[0]);
        Assert.AreEqual(expectedMax, columns[columns.Length - 1]);
        Assert.AreEqual(expectedMax - expectedMin + 1, columns.Length);
        for (int i = 1; i < columns.Length; i++)
            Assert.AreEqual(columns[i - 1] + 1, columns[i]);
    }

    [Test]
    public void BridgeColumns_WithoutShift_AreEmpty()
    {
        CollectionAssert.IsEmpty(TowerSegmentShiftMath.GetBridgeColumns(2, 2, 5));
    }


    [Test]
    public void BridgeChunks_SixCells_UseTwoExistingThreeCellRigidBodies()
    {
        CollectionAssert.AreEqual(
            new[] { 3, 3 },
            TowerSegmentShiftMath.GetBridgeChunkWidths(6, true, true));
    }

    [TestCase(2, 3, 5)]
    [TestCase(-1, -2, 5)]
    public void BridgeChunks_CoverExactLeftAndRightMinimalUnion(
        int previousOffset,
        int nextOffset,
        int layerWidth)
    {
        int[] columns = TowerSegmentShiftMath.GetBridgeColumns(
            previousOffset, nextOffset, layerWidth);
        int[] widths = TowerSegmentShiftMath.GetBridgeChunkWidths(
            columns.Length, true, true);

        Assert.AreEqual(columns.Length, Sum(widths));
        for (int i = 0; i < widths.Length; i++) Assert.Greater(widths[i], 0);
    }

    [Test]
    public void StructuralSupport_RegularActivationCannotMakeItDynamic_ButItCanBeDestroyed()
    {
        GameObject go = CreateTowerBlock("StructuralSupport");
        try
        {
            TowerBlock block = go.GetComponent<TowerBlock>();
            Rigidbody2D body = go.GetComponent<Rigidbody2D>();
            Collider2D collider = go.GetComponent<Collider2D>();

            block.ConfigureStructuralSupport();
            block.MakeDynamic();

            Assert.IsTrue(block.IsStructuralSupport);
            Assert.IsTrue(block.isStatic);
            Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType);

            block.DestroyBlock();

            Assert.IsTrue(block.IsDestroying);
            Assert.IsFalse(collider.enabled);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void TrapSelection_ExcludesStructuralSupport()
    {
        GameObject builderObject = new GameObject("Builder");
        GameObject bridgeObject = CreateTowerBlock("Bridge");
        bridgeObject.transform.SetParent(builderObject.transform);
        try
        {
            TowerBuilder builder = builderObject.AddComponent<TowerBuilder>();
            TowerBlock bridge = bridgeObject.GetComponent<TowerBlock>();
            bridge.ConfigureStructuralSupport();

            typeof(TowerBuilder).GetField("trapBlockChance", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(builder, 1f);
            typeof(TowerBuilder).GetMethod("TrySpawnTrapInBatch", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(builder, new object[] { -1f, 1f });

            Assert.IsNull(bridgeObject.GetComponent<TrapBlock>());
        }
        finally
        {
            Object.DestroyImmediate(builderObject);
        }
    }

    private static int Sum(int[] values)
    {
        int sum = 0;
        for (int i = 0; i < values.Length; i++) sum += values[i];
        return sum;
    }

    private static GameObject CreateTowerBlock(string name)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<BoxCollider2D>();
        TowerBlock block = go.AddComponent<TowerBlock>();
        typeof(TowerBlock).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(block, null);
        return go;
    }
}
