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


    private static void SetPrivateField(object target, string fieldName, object value)
    {
        typeof(TowerBuilder).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(target, value);
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


    [TestCase(0, 1, 2, 3, 2, 2)]
    [TestCase(2, 1, 2, 3, 3, 1)]
    [TestCase(3, 1, 2, 3, 1, 2)]
    [TestCase(-2, -1, 2, 3, -3, 1)]
    public void TwoCellStep_RespectsLimitAndDowngradesOrReverses(
        int currentOffset,
        int direction,
        int requestedStep,
        int limit,
        int expectedOffset,
        int expectedAppliedStep)
    {
        int resolvedDirection;
        int appliedStep;

        int result = TowerSegmentShiftMath.GetNextOffset(
            currentOffset, direction, requestedStep, limit, out resolvedDirection, out appliedStep);

        Assert.AreEqual(expectedOffset, result);
        Assert.AreEqual(expectedAppliedStep, appliedStep);
        Assert.LessOrEqual(System.Math.Abs(result), limit);
    }

    [Test]
    public void ProgressiveChance_IsZeroEarlyAndReachesCappedMaximum()
    {
        Assert.AreEqual(0f, TowerSegmentShiftMath.CalculateProgressiveChance(3, 4, 14, 0.18f));
        Assert.AreEqual(0.09f, TowerSegmentShiftMath.CalculateProgressiveChance(9, 4, 14, 0.18f), 0.0001f);
        Assert.AreEqual(0.18f, TowerSegmentShiftMath.CalculateProgressiveChance(20, 4, 14, 0.18f), 0.0001f);
    }

    [Test]
    public void DirectionRunBounds_GrowWithDepthAndStopAtCap()
    {
        int minimum;
        int maximum;

        TowerSegmentShiftMath.CalculateDirectionRunBounds(3, 2, 4, 4, 4, 3, out minimum, out maximum);
        Assert.AreEqual(2, minimum);
        Assert.AreEqual(4, maximum);

        TowerSegmentShiftMath.CalculateDirectionRunBounds(8, 2, 4, 4, 4, 3, out minimum, out maximum);
        Assert.AreEqual(4, minimum);
        Assert.AreEqual(6, maximum);

        TowerSegmentShiftMath.CalculateDirectionRunBounds(100, 2, 4, 4, 4, 3, out minimum, out maximum);
        Assert.AreEqual(5, minimum);
        Assert.AreEqual(7, maximum);
    }

    [TestCase(0, 2, 5, 7)]
    [TestCase(1, -1, 5, 7)]
    public void BridgeColumns_TwoCellStep_CoversExactContinuousUnion(
        int previousOffset,
        int nextOffset,
        int layerWidth,
        int expectedWidth)
    {
        int[] columns = TowerSegmentShiftMath.GetBridgeColumns(previousOffset, nextOffset, layerWidth);

        Assert.AreEqual(expectedWidth, columns.Length);
        for (int i = 1; i < columns.Length; i++)
            Assert.AreEqual(columns[i - 1] + 1, columns[i]);
        CollectionAssert.AreEqual(new[] { 4, 3 }, TowerSegmentShiftMath.GetBridgeChunkWidths(columns.Length, true, true));
    }

    [Test]
    public void BouncyRules_UseDepthCurveAndCappedBatchChance()
    {
        Assert.AreEqual(0f, TowerBouncyBlockRules.CalculateChance(2, 3, 14, 0.28f));
        Assert.AreEqual(0.14f, TowerBouncyBlockRules.CalculateChance(8, 3, 13, 0.28f), 0.0001f);
        Assert.AreEqual(0.28f, TowerBouncyBlockRules.CalculateChance(20, 3, 14, 0.28f), 0.0001f);
    }

    [TestCase(20, 4, 7, 0f, 4)]
    [TestCase(20, 4, 7, 1f, 7)]
    [TestCase(5, 4, 7, 1f, 5)]
    [TestCase(3, 4, 7, 0f, 3)]
    [TestCase(0, 4, 7, 0.5f, 0)]
    public void BouncyRules_BatchSizeRespectsMinimumMaximumAndEligibleCount(
        int eligible,
        int minimum,
        int maximum,
        float roll,
        int expected)
    {
        Assert.AreEqual(expected, TowerBouncyBlockRules.CalculateBatchSize(
            eligible, minimum, maximum, roll));
    }

    [TestCase(false, false, false, true)]
    [TestCase(true, false, false, false)]
    [TestCase(false, true, false, false)]
    [TestCase(false, false, true, false)]
    public void BouncyRules_ExcludeStructuralTrapAndRainbow(
        bool structural,
        bool trap,
        bool rainbow,
        bool expected)
    {
        Assert.AreEqual(expected, TowerBouncyBlockRules.IsEligible(structural, trap, rainbow));
    }

    [Test]
    public void BouncyRules_SelectClusterUsesMinimumLayerSpan()
    {
        int[] layers = { 0, 1, 1, 2, 10, 11, 30 };
        int[] selected = TowerBouncyBlockRules.SelectClusterIndices(layers, 4, 0);

        Assert.AreEqual(4, selected.Length);
        int minimum = int.MaxValue;
        int maximum = int.MinValue;
        for (int i = 0; i < selected.Length; i++)
        {
            minimum = System.Math.Min(minimum, layers[selected[i]]);
            maximum = System.Math.Max(maximum, layers[selected[i]]);
        }
        Assert.AreEqual(2, maximum - minimum);
    }

    [Test]
    public void TrySpawnBouncyBatch_ConfiguresAtMostOneBatchPerSegment()
    {
        GameObject builderObject = new GameObject("Builder");
        PhysicsMaterial2D material = new PhysicsMaterial2D("Test Bouncy") { friction = 0.50f, bounciness = 0.45f };
        try
        {
            TowerBuilder builder = builderObject.AddComponent<TowerBuilder>();
            SetPrivateField(builder, "bouncyBlockPhysicsMaterial", material);
            SetPrivateField(builder, "bouncyBatchStartSegment", 0);
            SetPrivateField(builder, "bouncyBatchFullChanceSegment", 1);
            SetPrivateField(builder, "maxBouncyBatchChance", 1f);
            SetPrivateField(builder, "minBouncyBlocksPerBatch", 4);
            SetPrivateField(builder, "maxBouncyBlocksPerBatch", 4);

            for (int i = 0; i < 8; i++)
            {
                GameObject block = CreateTowerBlock("Normal" + i);
                block.transform.SetParent(builderObject.transform);
                block.transform.position = new Vector3(i % 4, i / 4, 0f);
            }

            MethodInfo spawn = typeof(TowerBuilder).GetMethod(
                "TrySpawnBouncyBatch", BindingFlags.Instance | BindingFlags.NonPublic);
            spawn.Invoke(builder, new object[] { -1f, 3f, 1 });
            spawn.Invoke(builder, new object[] { -1f, 3f, 1 });

            TowerBlock[] blocks = builderObject.GetComponentsInChildren<TowerBlock>();
            int bouncyCount = 0;
            for (int i = 0; i < blocks.Length; i++)
                if (blocks[i].blockTypeName == "Bouncy Block") bouncyCount++;
            Assert.AreEqual(4, bouncyCount);
        }
        finally
        {
            Object.DestroyImmediate(builderObject);
            Object.DestroyImmediate(material);
        }
    }

    [Test]
    public void ApplyBouncyBlock_AssignsMaterialVisualNameAndRemainsDestroyable()
    {
        GameObject builderObject = new GameObject("Builder");
        GameObject blockObject = CreateTowerBlock("Normal");
        PhysicsMaterial2D material = new PhysicsMaterial2D("Test Bouncy") { friction = 0.50f, bounciness = 0.45f };
        try
        {
            TowerBuilder builder = builderObject.AddComponent<TowerBuilder>();
            TowerBlock block = blockObject.GetComponent<TowerBlock>();
            SetPrivateField(builder, "bouncyBlockPhysicsMaterial", material);

            typeof(TowerBuilder).GetMethod("ApplyBouncyBlock", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(builder, new object[] { block });

            Assert.AreSame(material, blockObject.GetComponent<Collider2D>().sharedMaterial);
            Assert.AreEqual("Bouncy Block", block.blockTypeName);
            Assert.AreEqual(new Color(0.45f, 1f, 0.08f, 1f), blockObject.GetComponent<SpriteRenderer>().color);
            block.MakeDynamic();
            Assert.AreEqual(RigidbodyType2D.Dynamic, blockObject.GetComponent<Rigidbody2D>().bodyType);
            block.DestroyBlock();
            Assert.IsTrue(block.IsDestroying);
        }
        finally
        {
            Object.DestroyImmediate(blockObject);
            Object.DestroyImmediate(builderObject);
            Object.DestroyImmediate(material);
        }
    }



    [Test]
    public void SegmentOffsetLookup_ReturnsRegisteredOffsetsAcrossTwoCellTransition()
    {
        GameObject builderObject = new GameObject("Builder");
        try
        {
            TowerBuilder builder = builderObject.AddComponent<TowerBuilder>();
            MethodInfo register = typeof(TowerBuilder).GetMethod(
                "RegisterSegmentOffsetZone", BindingFlags.Instance | BindingFlags.NonPublic);
            register.Invoke(builder, new object[] { -40f, -20f, 2 });
            register.Invoke(builder, new object[] { -20f, 0f, 0 });

            Assert.AreEqual(2, builder.GetSegmentXOffsetAtY(-30f));
            Assert.AreEqual(0, builder.GetSegmentXOffsetAtY(-10f));
            Assert.AreEqual(builder.layerWidth / 2f + 2f, builder.GetTowerCenterXAtY(-30f));
        }
        finally
        {
            Object.DestroyImmediate(builderObject);
        }
    }

    [Test]
    public void CreateSegmentBridge_TwoCellShiftUsesMinimumTwoKinematicClickableChunks()
    {
        GameObject builderObject = new GameObject("Builder");
        GameObject single = CreateTowerBlock("Single");
        GameObject line3 = CreateTowerBlock("Line3");
        GameObject line4 = CreateTowerBlock("Line4");
        try
        {
            TowerBuilder builder = builderObject.AddComponent<TowerBuilder>();
            builder.layerWidth = 5;
            builder.singleBlockPrefab = single;
            builder.line3BlockPrefab = line3;
            builder.lineBlockPrefab = line4;

            typeof(TowerBuilder).GetMethod("CreateSegmentBridge", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(builder, new object[] { 0, 2, -20f });

            Assert.AreEqual(2, builderObject.transform.childCount);
            for (int i = 0; i < builderObject.transform.childCount; i++)
            {
                TowerBlock block = builderObject.transform.GetChild(i).GetComponent<TowerBlock>();
                Assert.IsTrue(block.IsStructuralSupport);
                Assert.AreEqual(RigidbodyType2D.Kinematic, block.GetComponent<Rigidbody2D>().bodyType);
                Assert.IsTrue(block.GetComponent<Collider2D>().enabled);
            }
        }
        finally
        {
            Object.DestroyImmediate(builderObject);
            Object.DestroyImmediate(single);
            Object.DestroyImmediate(line3);
            Object.DestroyImmediate(line4);
        }
    }
}
