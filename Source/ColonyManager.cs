using RimWorld;
using Verse;
using System.Collections.Generic; // Add this using directive

public class ColonyManager
{
    public void UpdatePawnCountForCurrentColony()
    {
        // Get the current colony
        Map currentMap = Find.CurrentMap;
        if (currentMap == null)
        {
            Log.Error("No current map found.");
            return;
        }

        // Get the list of pawns in the current colony
        IReadOnlyList<Pawn> pawns = currentMap.mapPawns.AllPawnsSpawned;

        // Set or modify the pawn count
        int pawnCount = pawns.Count; // Correctly access the Count property
        Log.Message($"Current colony has {pawnCount} pawns.");

        // Example: Perform some action based on pawn count
        if (pawnCount < 10)
        {
            Log.Message("Colony is small, consider recruiting more pawns.");
        }
        else
        {
            Log.Message("Colony has a sufficient number of pawns.");
        }
    }
}