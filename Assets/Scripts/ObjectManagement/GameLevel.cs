using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLevel : PersistableObject {

    [SerializeField]
    PersistableObject[] persistentObjects;

    public static GameLevel Current { get; private set; }

    public Vector3 SpawnPoint {
        get {
            return spawnZone.SpawnPoint;
        }
    }

    [SerializeField]
    SpawnZone spawnZone;

    void OnEnable () {
        Current = this;
        if (persistentObjects == null) {
            persistentObjects = new PersistableObject[0];
        }
    }

    public override void Save(GameDataWriter writer) {
        writer.Write(persistentObjects.Length);
        for (int i = 0; i < persistentObjects.Length; i++) {
            persistentObjects[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader) {
        int savedCount = reader.ReadInt();
        for (int i = 0; i < savedCount; i++) {
            persistentObjects[i].Load(reader);
        }
    }
}
