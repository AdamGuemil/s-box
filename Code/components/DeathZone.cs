using Sandbox;

public sealed class DeathZone : Component
{
    [Property] public float DeathHeight { get; set; } = -100f;
    [Property] public Vector3 RespawnPosition { get; set; } = new Vector3(0, 0, 100);
    
    protected override void OnUpdate()
    {
        // Utilise directement la position de CET objet
        
        if (Transform.Position.z < DeathHeight)
        {
            Log.Info("MORT ! Respawn...");
            Transform.Position = RespawnPosition;
        }
    }
}