using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;

public class AccountRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Unique, Collation("NOCASE"), NotNull, MaxLength(32)] public string Name { get; set; }
    [NotNull] public byte[] PasswordHash { get; set; }
    [NotNull] public byte[] PasswordSalt { get; set; }
    public int HashRounds { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class CharacterRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int AccountId { get; set; }
    public string Name { get; set; }
    public int Essence { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class ItemRow
{
    [PrimaryKey] public string InstanceId { get; set; }
    public int CharacterId { get; set; }
    public string Template { get; set; }
    public int? BagX { get; set; }
    public int? BagY { get; set; }
    public string Slot { get; set; }
}

public class LearnedAbilityRow
{
    public int CharacterId { get; set; }
    public string AbilityName { get; set; }
}

public class CharacterRows
{
    public int Essence;
    public readonly Dictionary<string, ItemRow> Items = new Dictionary<string, ItemRow>();
    public readonly HashSet<string> AbilityNames = new HashSet<string>();

    public void AddItem(ItemRow row)
    {
        if (Items.TryAdd(row.InstanceId, row)) return;
        Debug.LogError($"CharacterRows: character {row.CharacterId} has item {row.InstanceId} ({row.Template}) twice; keeping the first.");
    }
}

public class ServerDatabase
{
    public static string DefaultPath => Path.Combine(Application.persistentDataPath, "rebirth.db");

    public SQLiteConnection Connection => connection;

    private readonly SQLiteConnection connection;

    private static readonly string[] CreateTableStatements =
    {
        @"CREATE TABLE IF NOT EXISTS CharacterRow (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountId  INTEGER NOT NULL REFERENCES AccountRow(Id) ON DELETE CASCADE,
    Name       TEXT    NOT NULL COLLATE NOCASE CHECK (length(Name) <= 32),
    Essence    INTEGER NOT NULL DEFAULT 0 CHECK (Essence >= 0),
    CreatedUtc BIGINT  NOT NULL
)",
        "CREATE INDEX IF NOT EXISTS CharacterRow_AccountId ON CharacterRow (AccountId)",
        @"CREATE TABLE IF NOT EXISTS ItemRow (
    InstanceId  TEXT    NOT NULL PRIMARY KEY,
    CharacterId INTEGER NOT NULL REFERENCES CharacterRow(Id) ON DELETE CASCADE,
    Template    TEXT    NOT NULL,
    BagX        INTEGER NULL,
    BagY        INTEGER NULL,
    Slot        TEXT    NULL,
    UNIQUE (CharacterId, Slot),
    CHECK (
        (BagX IS NOT NULL AND BagY IS NOT NULL AND Slot IS NULL) OR
        (BagX IS NULL     AND BagY IS NULL     AND Slot IS NOT NULL) OR
        (BagX IS NULL     AND BagY IS NULL     AND Slot IS NULL)
    )
)",
        @"CREATE UNIQUE INDEX IF NOT EXISTS ItemRow_OneHeld
    ON ItemRow (CharacterId) WHERE BagX IS NULL AND Slot IS NULL",
        @"CREATE TABLE IF NOT EXISTS LearnedAbilityRow (
    CharacterId INTEGER NOT NULL REFERENCES CharacterRow(Id) ON DELETE CASCADE,
    AbilityName TEXT    NOT NULL,
    PRIMARY KEY (CharacterId, AbilityName)
)"
    };

    public ServerDatabase(string path)
    {
        connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, true);
        TurnOnSafetySettings();
        BuildTables();
    }

    public AccountRow FindAccount(string accountName)
    {
        return connection.Find<AccountRow>(account => account.Name == accountName);
    }

    public void AddAccount(AccountRow account)
    {
        connection.Insert(account);
    }

    public List<CharacterRow> CharactersOf(int accountId)
    {
        return connection.Table<CharacterRow>().Where(character => character.AccountId == accountId).OrderBy(character => character.Id).ToList();
    }

    public CharacterRow AddCharacter(int accountId, string characterName)
    {
        var character = new CharacterRow { AccountId = accountId, Name = characterName, CreatedUtc = DateTime.UtcNow };
        connection.Insert(character);
        return character;
    }

    public void Close()
    {
        connection.Close();
    }

    private void TurnOnSafetySettings()
    {
        connection.Execute("PRAGMA foreign_keys = ON");
        connection.EnableWriteAheadLogging();
        connection.Execute("PRAGMA synchronous = NORMAL");
        if (connection.ExecuteScalar<int>("PRAGMA foreign_keys") != 1)
            Debug.LogError("ServerDatabase: foreign keys are off, so deleting a character will leave its items and abilities behind.");
    }

    private void BuildTables()
    {
        connection.CreateTable<AccountRow>();
        foreach (string statement in CreateTableStatements)
            connection.Execute(statement);
    }
}

public class CharacterStore
{
    private readonly SQLiteConnection connection;
    private readonly int characterId;
    private Dictionary<string, ItemRow> savedItems = new Dictionary<string, ItemRow>();
    private HashSet<string> savedAbilities = new HashSet<string>();

    public CharacterStore(SQLiteConnection connection, int characterId)
    {
        this.connection = connection;
        this.characterId = characterId;
    }

    public int SavedEssence { get; private set; }

    public CharacterRows Load()
    {
        try { return LoadRows(); }
        catch (SQLiteException failure) { return ReportLoadFailure(failure); }
    }

    private CharacterRows LoadRows()
    {
        if (!CharacterExists()) return ReportMissingCharacter();
        CharacterRows rows = ReadRows();
        RememberAsSaved(rows);
        return rows;
    }

    private CharacterRows ReportLoadFailure(SQLiteException failure)
    {
        Debug.LogError($"CharacterStore: loading character {characterId} failed ({failure.Result}): {failure.Message}");
        return null;
    }

    public bool Write(CharacterRows rows)
    {
        if (!TryWriteDifferences(rows)) return false;
        RememberAsSaved(rows);
        return true;
    }

    private bool CharacterExists()
    {
        return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM CharacterRow WHERE Id = ?", characterId) > 0;
    }

    private CharacterRows ReportMissingCharacter()
    {
        Debug.LogError($"CharacterStore: there is no character {characterId} to load, so nothing will be saved.");
        return null;
    }

    private CharacterRows ReadRows()
    {
        var rows = new CharacterRows { Essence = connection.ExecuteScalar<int>("SELECT Essence FROM CharacterRow WHERE Id = ?", characterId) };
        foreach (ItemRow item in connection.Query<ItemRow>("SELECT * FROM ItemRow WHERE CharacterId = ?", characterId))
            rows.AddItem(item);
        foreach (LearnedAbilityRow ability in connection.Query<LearnedAbilityRow>("SELECT * FROM LearnedAbilityRow WHERE CharacterId = ? ORDER BY AbilityName", characterId))
            rows.AbilityNames.Add(ability.AbilityName);
        return rows;
    }

    private void RememberAsSaved(CharacterRows rows)
    {
        savedItems = rows.Items;
        savedAbilities = rows.AbilityNames;
        SavedEssence = rows.Essence;
    }

    private bool TryWriteDifferences(CharacterRows rows)
    {
        try { connection.RunInTransaction(() => WriteDifferences(rows)); return true; }
        catch (SQLiteException failure) { ReportFailure(rows, failure); return false; }
    }

    private void WriteDifferences(CharacterRows rows)
    {
        WriteEssence(rows.Essence);
        DeleteStaleItems(rows.Items);
        InsertFreshItems(rows.Items);
        DeleteForgottenAbilities(rows.AbilityNames);
        InsertNewAbilities(rows.AbilityNames);
    }

    private void WriteEssence(int essence)
    {
        if (essence == SavedEssence) return;
        connection.Execute("UPDATE CharacterRow SET Essence = ? WHERE Id = ?", essence, characterId);
    }

    private void DeleteStaleItems(Dictionary<string, ItemRow> currentItems)
    {
        foreach (ItemRow row in savedItems.Values)
            if (!IsUnchanged(row, currentItems))
                connection.Delete<ItemRow>(row.InstanceId);
    }

    private void InsertFreshItems(Dictionary<string, ItemRow> currentItems)
    {
        foreach (ItemRow row in currentItems.Values)
            if (!IsUnchanged(row, savedItems))
                connection.Insert(row);
    }

    private static bool IsUnchanged(ItemRow row, Dictionary<string, ItemRow> otherItems)
    {
        return otherItems.TryGetValue(row.InstanceId, out ItemRow match) && SameRow(row, match);
    }

    private static bool SameRow(ItemRow row, ItemRow other)
    {
        return row.Template == other.Template && row.BagX == other.BagX && row.BagY == other.BagY && row.Slot == other.Slot;
    }

    private void DeleteForgottenAbilities(HashSet<string> currentAbilities)
    {
        foreach (string abilityName in savedAbilities)
            if (!currentAbilities.Contains(abilityName))
                connection.Execute("DELETE FROM LearnedAbilityRow WHERE CharacterId = ? AND AbilityName = ?", characterId, abilityName);
    }

    private void InsertNewAbilities(HashSet<string> currentAbilities)
    {
        foreach (string abilityName in currentAbilities)
            if (!savedAbilities.Contains(abilityName))
                connection.Insert(new LearnedAbilityRow { CharacterId = characterId, AbilityName = abilityName });
    }

    private void ReportFailure(CharacterRows rows, SQLiteException failure)
    {
        Debug.LogError($"CharacterStore: saving character {characterId} failed ({failure.Result}): {failure.Message}");
        foreach (ItemRow row in rows.Items.Values)
            ReportIfOwnedElsewhere(row);
    }

    private void ReportIfOwnedElsewhere(ItemRow row)
    {
        int ownerId = connection.ExecuteScalar<int>("SELECT CharacterId FROM ItemRow WHERE InstanceId = ?", row.InstanceId);
        if (ownerId == 0 || ownerId == characterId) return;
        Debug.LogError($"CharacterStore: item {row.InstanceId} ({row.Template}) is saved under character {ownerId}, not character {characterId} who is holding it.");
    }
}
