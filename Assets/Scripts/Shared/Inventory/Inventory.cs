using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : NetworkBehaviour
{
    public class PlacedItem
    {
        public readonly Item Item;
        public Vector2Int Origin;

        public PlacedItem(Item item, Vector2Int origin)
        {
            Item = item;
            Origin = origin;
        }
    }

    public struct StoredItem
    {
        public string Template;
        public string InstanceId;
        public int X;
        public int Y;
    }

    [Header("Grid")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 11;

    [Header("Dropping")]
    [SerializeField, Range(0f, 3f)] private float dropScatter = 0.6f;

    public UnityEvent onChanged = new UnityEvent();

    private readonly SyncList<StoredItem> stored = new SyncList<StoredItem>();

    private readonly SyncVar<StoredItem> onCursor = new SyncVar<StoredItem>();

    private readonly List<PlacedItem> swapScratch = new List<PlacedItem>();
    private Item cursorItem;

    private PlacedItem[,] occupancy;
    private readonly List<PlacedItem> placed = new List<PlacedItem>();

    private int suppressDepth;
    private bool changedWhileSuppressed;

    public int Columns => columns;
    public int Rows => rows;
    public IReadOnlyList<PlacedItem> Placed => placed;

    public Item OnCursor => IsServerInitialized ? cursorItem : Resolve(onCursor.Value);

    private void Awake()
    {
        occupancy = new PlacedItem[columns, rows];
        stored.OnChange += HandleStoredChanged;
        onCursor.OnChange += HandleCursorChanged;
    }

    private void OnDestroy()
    {
        stored.OnChange -= HandleStoredChanged;
        onCursor.OnChange -= HandleCursorChanged;
    }

    private void HandleCursorChanged(StoredItem previous, StoredItem next, bool asServer)
    {
        if (!asServer) onChanged.Invoke();
    }

    public override void OnStartClient()
    {
        if (!IsServerInitialized) RebuildFromStored();
    }

    private void HandleStoredChanged(SyncListOperation operation, int index, StoredItem previous,
                                     StoredItem next, bool asServer)
    {
        if (asServer) return;

        if (operation != SyncListOperation.Complete) return;

        RebuildFromStored();
    }

    private void RebuildFromStored()
    {
        placed.Clear();
        System.Array.Clear(occupancy, 0, occupancy.Length);

        foreach (StoredItem entry in stored)
        {
            ItemDefinition definition = ItemDatabase.Instance != null
                ? ItemDatabase.Instance.Find(entry.Template)
                : null;

            if (definition == null)
            {
                Debug.LogWarning($"Inventory: no item asset named '{entry.Template}', so it is not in the bag.");
                continue;
            }

            PlacedItem record = new PlacedItem(new Item(definition, entry.InstanceId),
                                               new Vector2Int(entry.X, entry.Y));
            placed.Add(record);
            OccupyCells(record);
        }

        NotifyChanged();
    }

    private Item Resolve(StoredItem entry)
    {
        if (string.IsNullOrEmpty(entry.Template)) return null;

        ItemDefinition definition = ItemDatabase.Instance != null
            ? ItemDatabase.Instance.Find(entry.Template)
            : null;

        if (definition == null)
        {
            Debug.LogWarning($"Inventory: no item asset named '{entry.Template}'.");
            return null;
        }

        return new Item(definition, entry.InstanceId);
    }

    private static StoredItem Describe(Item item, Vector2Int origin)
    {
        if (item == null) return default;

        return new StoredItem
        {
            Template = item.Definition.name,
            InstanceId = item.InstanceId,
            X = origin.x,
            Y = origin.y
        };
    }

    private void WriteStored()
    {
        stored.Clear();

        foreach (PlacedItem record in placed)
        {
            stored.Add(new StoredItem
            {
                Template = record.Item.Definition.name,
                InstanceId = record.Item.InstanceId,
                X = record.Origin.x,
                Y = record.Origin.y
            });
        }
    }

    public PlacedItem ItemAt(Vector2Int cell)
    {
        if (!InBounds(cell)) return null;
        return occupancy[cell.x, cell.y];
    }

    private bool InBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.y >= 0 && cell.x < columns && cell.y < rows;

    public bool CanPlace(Item item, Vector2Int origin, PlacedItem ignore = null) =>
        FootprintInBounds(origin, item.Size) && FootprintIsClear(origin, item.Size, ignore);

    private bool FootprintInBounds(Vector2Int origin, Vector2Int size)
    {
        if (origin.x < 0 || origin.y < 0) return false;
        return origin.x + size.x <= columns && origin.y + size.y <= rows;
    }

    private bool FootprintIsClear(Vector2Int origin, Vector2Int size, PlacedItem ignore)
    {
        for (int x = origin.x; x < origin.x + size.x; x++)
        {
            for (int y = origin.y; y < origin.y + size.y; y++)
            {
                PlacedItem occupant = occupancy[x, y];
                if (occupant != null && occupant != ignore) return false;
            }
        }
        return true;
    }

    public PlacedItem FindPlacement(Item item)
    {
        foreach (PlacedItem record in placed)
        {
            if (record.Item == item) return record;
        }
        return null;
    }

    public PlacedItem FindByInstanceId(string instanceId)
    {
        foreach (PlacedItem record in placed)
        {
            if (record.Item.InstanceId == instanceId) return record;
        }
        return null;
    }

    public void DistinctOccupantsUnder(Vector2Int origin, Vector2Int size, List<PlacedItem> results, PlacedItem ignore = null)
    {
        results.Clear();
        for (int x = origin.x; x < origin.x + size.x; x++)
        {
            for (int y = origin.y; y < origin.y + size.y; y++)
            {
                if (x < 0 || y < 0 || x >= columns || y >= rows) continue;
                PlacedItem occupant = occupancy[x, y];
                if (occupant == null || occupant == ignore) continue;
                if (!results.Contains(occupant)) results.Add(occupant);
            }
        }
    }

    public void RequestLift(string instanceId)
    {
        if (IsServerInitialized) LiftOnServer(instanceId);
        else ServerLift(instanceId);
    }

    public void RequestPlaceFromCursor(Vector2Int origin)
    {
        if (IsServerInitialized) PlaceFromCursorOnServer(origin);
        else ServerPlaceFromCursor(origin);
    }

    public void RequestSort()
    {
        if (IsServerInitialized) SortOnServer();
        else ServerSort();
    }

    [ServerRpc]
    private void ServerLift(string instanceId) => LiftOnServer(instanceId);

    [ServerRpc]
    private void ServerPlaceFromCursor(Vector2Int origin) => PlaceFromCursorOnServer(origin);

    public void RequestReturnCursor()
    {
        if (IsServerInitialized) ReturnCursorOnServer();
        else ServerReturnCursor();
    }

    public void RequestThrowFromCursor()
    {
        if (IsServerInitialized) ThrowFromCursorOnServer();
        else ServerThrowFromCursor();
    }

    [ServerRpc]
    private void ServerSort() => SortOnServer();

    [ServerRpc]
    private void ServerThrowFromCursor() => ThrowFromCursorOnServer();

    [ServerRpc]
    private void ServerReturnCursor() => ReturnCursorOnServer();

    private void ReturnCursorOnServer()
    {
        if (cursorItem == null) return;

        Item carried = cursorItem;

        BeginBatch();
        SetCursor(null);
        if (!TryAddItem(carried))
        {
            SetCursor(carried);
            ThrowFromCursorOnServer();
        }
        EndBatch();
    }

    private void ThrowFromCursorOnServer()
    {
        if (cursorItem == null) return;

        Vector2 scatter = Random.insideUnitCircle * dropScatter;
        Vector3 offset = new Vector3(scatter.x, 0.0f, scatter.y);

        if (GroundItem.Spawn(transform.position, offset, cursorItem, transform)) SetCursor(null);
    }

    private void LiftOnServer(string instanceId)
    {
        if (cursorItem != null) return;

        PlacedItem record = FindByInstanceId(instanceId);
        if (record == null) return;

        BeginBatch();
        RemovePlaced(record);
        SetCursor(record.Item);
        EndBatch();
    }

    private void PlaceFromCursorOnServer(Vector2Int origin)
    {
        if (cursorItem == null) return;

        Item carried = cursorItem;

        if (CanPlace(carried, origin))
        {
            BeginBatch();
            SetCursor(null);
            PlaceAt(carried, origin);
            EndBatch();
            return;
        }

        if (!FootprintInBounds(origin, carried.Size)) return;
        DistinctOccupantsUnder(origin, carried.Size, swapScratch);
        if (swapScratch.Count != 1) return;

        PlacedItem under = swapScratch[0];

        BeginBatch();
        RemovePlaced(under);
        PlaceAt(carried, origin);
        SetCursor(under.Item);
        EndBatch();
    }

    private void SortOnServer()
    {
        if (!IsServerInitialized || placed.Count == 0) return;

        List<(Item Item, Vector2Int Origin)> snapshot = new List<(Item, Vector2Int)>();
        List<Item> order = new List<Item>();

        foreach (PlacedItem record in placed)
        {
            snapshot.Add((record.Item, record.Origin));
            order.Add(record.Item);
        }
        order.Sort(CompareForSort);

        BeginBatch();
        EmptyGrid();

        bool packed = true;
        foreach (Item item in order)
        {
            if (TryAddItem(item)) continue;

            packed = false;
            break;
        }

        if (!packed)
        {
            EmptyGrid();
            foreach ((Item item, Vector2Int origin) in snapshot) PlaceAt(item, origin);
        }
        EndBatch();
    }

    private void EmptyGrid()
    {
        for (int index = placed.Count - 1; index >= 0; index--) RemovePlaced(placed[index]);
    }

    private static int CompareForSort(Item a, Item b)
    {
        int areaA = a.Size.x * a.Size.y;
        int areaB = b.Size.x * b.Size.y;
        if (areaA != areaB) return areaB - areaA;
        if (a.Size.y != b.Size.y) return b.Size.y - a.Size.y;

        int rarity = b.Definition.rarity.CompareTo(a.Definition.rarity);
        if (rarity != 0) return rarity;

        return string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase);
    }

    public void SetCursor(Item item)
    {
        if (!ServerOwnsThis("put something on the cursor")) return;

        cursorItem = item;
        onCursor.Value = Describe(item, Vector2Int.zero);
        NotifyChanged();
    }

    public PlacedItem PlaceAt(Item item, Vector2Int origin)
    {
        if (!ServerOwnsThis("put an item in the bag")) return null;

        PlacedItem record = new PlacedItem(item, origin);
        placed.Add(record);
        OccupyCells(record);
        NotifyChanged();
        return record;
    }

    public void Clear()
    {
        if (!ServerOwnsThis("empty the bag")) return;

        BeginBatch();

        placed.Clear();
        System.Array.Clear(occupancy, 0, occupancy.Length);

        NotifyChanged();
        EndBatch();
    }

    public void RemovePlaced(PlacedItem record)
    {
        if (!ServerOwnsThis("take an item out of the bag")) return;
        if (record == null || !placed.Remove(record)) return;

        ClearCells(record);
        NotifyChanged();
    }

    public bool TryAddItem(Item item)
    {
        if (!ServerOwnsThis("put an item in the bag")) return false;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector2Int origin = new Vector2Int(x, y);
                if (CanPlace(item, origin))
                {
                    PlaceAt(item, origin);
                    return true;
                }
            }
        }
        return false;
    }

    private bool ServerOwnsThis(string what)
    {
        if (IsServerInitialized) return true;

        Debug.LogWarning($"Inventory: a client cannot {what} for {name} — ask through the Request methods.", this);
        return false;
    }

    private void OccupyCells(PlacedItem record) => WriteCells(record, record);

    private void ClearCells(PlacedItem record) => WriteCells(record, null);

    private void WriteCells(PlacedItem record, PlacedItem value)
    {
        Vector2Int size = record.Item.Size;
        for (int x = record.Origin.x; x < record.Origin.x + size.x; x++)
        {
            for (int y = record.Origin.y; y < record.Origin.y + size.y; y++)
            {
                if (x >= 0 && y >= 0 && x < columns && y < rows) occupancy[x, y] = value;
            }
        }
    }

    public void BeginBatch() => suppressDepth++;

    public void EndBatch()
    {
        suppressDepth = Mathf.Max(0, suppressDepth - 1);
        if (suppressDepth == 0 && changedWhileSuppressed)
        {
            changedWhileSuppressed = false;
            Announce();
        }
    }

    private void NotifyChanged()
    {
        if (suppressDepth > 0) { changedWhileSuppressed = true; return; }
        Announce();
    }

    private void Announce()
    {
        if (IsServerInitialized) WriteStored();
        onChanged.Invoke();
    }
}
