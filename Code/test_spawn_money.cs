using Sandbox;
using System.Linq;

public sealed class SpawnBlockComponent : Component
{
    [Property]
    public GameObject BlockPrefab { get; set; }

    [Property]
    public string SpawnKey { get; set; } = "e";
    
    [Property]
    public float SpawnDistance { get; set; } = 2;

    protected override void OnUpdate()
    {
        // Vérifie que la touche est appuyée
        if (!Input.Keyboard.Pressed(SpawnKey))
            return;

        if (BlockPrefab == null)
        {
            Log.Warning("BlockPrefab n'est pas assigné !");
            return;
        }

        // Trouve le GameObject enfant appelé "Body"
        var bodyObject = GameObject.Children.FirstOrDefault(child => child.Name == "Body");
        
        if (bodyObject == null)
        {
            Log.Warning("GameObject 'Body' non trouvé dans les enfants !");
            return;
        }

        Log.Info($"Body trouvé: {bodyObject.Name}");

        // Spawn devant le Body (rotation du corps)
        var spawnPos = bodyObject.Transform.Position + bodyObject.Transform.Rotation.Forward * SpawnDistance;
        spawnPos = spawnPos.WithZ(spawnPos.z + 35f);
        
        // Clone le prefab avec la rotation du Body
        var spawnedBlock = BlockPrefab.Clone(spawnPos, bodyObject.Transform.Rotation);
        
        Log.Info($"Bloc spawné à {spawnPos} !");
    }
}