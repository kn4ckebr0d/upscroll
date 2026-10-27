using System.Collections.Generic;
using UnityEngine;

/// Pickups: coins in patterns across the columns (water drops on hot planets),
/// and a rare free heart.
public partial class ScrollyGame
{
    enum PickupKind { Coin, Water, Heart }

    class Pickup
    {
        public PickupKind Kind;
        public int Column;
        public float Y;
        public GameObject Node;
    }

    readonly List<Pickup> coins = new();
    int runCoins;
    int heartZone = -1;   // the zone hiding this run's free heart, or -1 for none

    void RollFreeHeart()
    {
        heartZone = Random.value < Tuning.FreeHeartChance ? Random.Range(1, 7) : -1;
    }

    /// Coin patterns between checkpoints: straight lines, diagonals and zigzags.
    void AddCoins(float lower, float upper)
    {
        float y = lower + 3f;
        float top = upper - 1.5f;
        int last = Tuning.Columns - 1;
        while (y < top)
        {
            int pattern = Random.Range(0, 3);
            int length = Random.Range(3, 7);
            int c = Random.Range(0, Tuning.Columns);
            int direction = c < Tuning.Columns / 2 ? 1 : -1;
            for (int i = 0; i < length; i++)
            {
                float cy = y + i * Tuning.CoinSpacing;
                if (cy >= top) break;
                SpawnPickup(EffectAt(cy) == PlanetEffect.Hot ? PickupKind.Water : PickupKind.Coin, c, cy);
                switch (pattern)
                {
                    case 1: c = Mathf.Clamp(c + direction, 0, last); break;                              // diagonal
                    case 2: if (i % 2 == 1) c = Mathf.Clamp(c + (i % 4 == 1 ? 1 : -1), 0, last); break;   // zigzag
                }
            }
            y += length * Tuning.CoinSpacing + Random.Range(Tuning.CoinGapMin, Tuning.CoinGapMax);
        }

        if (ZoneIndex(lower + 0.01f) == heartZone)
        {
            float hy = Random.Range(lower + 6f, upper - 4f);
            SpawnPickup(PickupKind.Heart, Random.Range(0, Tuning.Columns), hy);
            heartZone = -1;
        }
    }

    void SpawnPickup(PickupKind kind, int c, float y)
    {
        if (SolidAt(c, y, y + 1.2f)) return;   // not inside rock
        var position = new Vector3(ColumnX(c), y + 0.6f, -0.75f);
        GameObject node;
        switch (kind)
        {
            case PickupKind.Water:
                node = new GameObject("Water");
                node.transform.SetParent(world, false);
                node.transform.position = position;
                Voxel.Box(node.transform, new Vector3(0.26f, 0.26f, 0.26f), 0x5BC0F8, Vector3.zero, castShadows: false);
                Voxel.Box(node.transform, new Vector3(0.14f, 0.16f, 0.14f), 0x5BC0F8, new Vector3(0, 0.2f, 0), castShadows: false);
                Voxel.Box(node.transform, new Vector3(0.06f, 0.1f, 0.02f), 0xE8F7FF, new Vector3(-0.06f, 0.03f, -0.14f), castShadows: false);
                break;
            case PickupKind.Heart:
                node = new GameObject("Heart");
                node.transform.SetParent(world, false);
                node.transform.position = position;
                // A little pixel heart made of voxels.
                int[][] rowsOfHeart = { new[] { 0 }, new[] { -1, 0, 1 }, new[] { -2, -1, 0, 1, 2 }, new[] { -2, -1, 1, 2 } };
                for (int row = 0; row < rowsOfHeart.Length; row++)
                    foreach (int col in rowsOfHeart[row])
                        Voxel.Box(node.transform, Vector3.one * 0.13f, 0xF06A7A, new Vector3(col * 0.13f, (row - 1.5f) * 0.13f, 0), castShadows: false);
                break;
            default:
                node = Voxel.Disc(world, 0.42f, 0.08f, coinMaterial, position);
                break;
        }
        node.AddComponent<Spinner>().DegreesPerSecond = kind == PickupKind.Heart ? 120 : 300;
        coins.Add(new Pickup { Kind = kind, Column = c, Y = y, Node = node });
    }

    void CollectCoins()
    {
        var p = climber.transform.position;
        coins.RemoveAll(pickup =>
        {
            var c = pickup.Node.transform.position;
            if (Mathf.Abs(c.x - p.x) > 0.5f || Mathf.Abs(c.y - (p.y + 0.6f)) > 0.6f) return false;
            switch (pickup.Kind)
            {
                case PickupKind.Coin:
                    runCoins++;
                    Progress.Coins++;
                    Synth.Instance.Coin();
                    Haptics.Tick(0.6f);
                    FlyCoin(pickup.Node.transform.position);
                    break;
                case PickupKind.Water:
                    stamina = Mathf.Min(1, stamina + Tuning.WaterDropStamina);
                    Synth.Instance.ClimbBlip(14, Tuning.ToneSteps);
                    Haptics.Tick(0.5f);
                    break;
                case PickupKind.Heart:
                    if (Progress.ExtraLives < Tuning.MaxExtraLives)
                    {
                        Progress.ExtraLives++;
                        ShowBanner("LUCKY YOU!", "FREE HEART!");
                    }
                    else
                    {
                        Progress.Coins += 25;
                        ShowBanner("HEARTS FULL", "+25 COINS");
                    }
                    Progress.Save();
                    Synth.Instance.Purchase();
                    Haptics.Success();
                    RefreshShop();
                    break;
            }
            pickup.Node.GetComponent<Spinner>().DegreesPerSecond = 1400;
            pickup.Node.AddComponent<CoinPop>();
            return true;
        });
    }

    void CullCoins(float bottom)
    {
        coins.RemoveAll(pickup =>
        {
            if (pickup.Y >= bottom) return false;
            Destroy(pickup.Node);
            return true;
        });
    }
}
