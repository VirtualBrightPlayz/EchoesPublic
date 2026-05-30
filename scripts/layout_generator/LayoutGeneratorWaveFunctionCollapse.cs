using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

public enum ELayoutGenerationResult : byte
{
    Success = 0,
    SpawningRequierdInstanceFailed = 1,
    SolvingContradictionFailed = 2,
    PostSpawnValidationFailed = 3,
    Other = 4
}

public partial class LayoutGeneratorWaveFunctionCollapse : Node
{
    private struct CellPossibility
    {
        public RoomGenerationSettings RoomGenerationResource;
        public int Rotation;

        public CellPossibility(RoomGenerationSettings roomGenerationResource, int rotation)
        {
            this.RoomGenerationResource = roomGenerationResource;
            this.Rotation = rotation % 4;
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.RoomGenerationResource.GetHashCode(), this.Rotation);
        }

        public static bool operator==(CellPossibility self, CellPossibility other)
        {
            return self.Equals(other);
        }

		public static bool operator !=(CellPossibility self, CellPossibility other)
		{
			return !self.Equals(other);
		}

		public override string ToString()
        {
            return $"[RoomGenerationResource: {this.RoomGenerationResource.ResourcePath}, Rotation: {this.Rotation}]";
        }
    }

    public override void _Ready()
    {
        this.Name = $"{this.GetParent().Name}_LayoutGeneratorWFC";
    }

    public async Task<ELayoutGenerationResult> GenerateAsync(Layout layout, RoomGenerationSettingsCollection settingCollection, ulong seed)
    {
        if (layout == null || !layout.IsInitialized) return ELayoutGenerationResult.Other;
        if (settingCollection == null || settingCollection.Collection.Count == 0) return ELayoutGenerationResult.Other;

        Log.Print($"{this.Name}: Starting layout generation for {layout.Name} with seed {seed}");

        ulong startTime = Time.GetTicksMsec();
        RandomNumberGenerator randomStream = new RandomNumberGenerator();
        randomStream.Seed = seed;

        // Get all possible room resources and save them
        var requiredInstances = new Dictionary<RoomGenerationSettings, int>();
        var maximumInstances = new Dictionary<RoomGenerationSettings, int>();
        foreach (var setting in settingCollection.Collection) 
        {
            requiredInstances.Add(setting, setting.RequiredInstances);
            maximumInstances.Add(setting, setting.MaximumInstances);
        }

        // Initiate and fill cell possibilities
        var cellPossibilities = new Dictionary<Vector2I, List<CellPossibility>>();
        for (int x = 0; x < layout.GridSize.X; x++)
        {
            for (int y = 0; y < layout.GridSize.Y; y++)
            {
                cellPossibilities.Add(new Vector2I(x, y), new List<CellPossibility>());
            }
        }

        // Select random cells and try to generate required room resources on them
        foreach (RoomGenerationSettings setting in requiredInstances.Keys)
        {
            List<Vector2I> untestedCells = cellPossibilities.Keys.ToList();
            while (requiredInstances[setting] > 0)
            {
                if (untestedCells.Count == 0)
                {
                    Log.PrintWarn(
                        $"{this.Name}: Required instance {setting.ResourcePath} could not be generated. Aborting...");
                    return ELayoutGenerationResult.SpawningRequierdInstanceFailed;
                }

                // Choose a random cell
                LayoutCell selectedCell = layout.GetCell(untestedCells[randomStream.RandiRange(0, untestedCells.Count - 1)]);

                // Check every rotation until we find a valid one
                int initialRotation = randomStream.RandiRange(0, 3);
                for (int i = initialRotation; i < initialRotation + 4; i++)
                {
                    bool isValid = !selectedCell.IsGenerated && selectedCell.IsRoomGenerationResourceValid(setting, i);
                    if (isValid) 
                    {
                        // Set cell to room resource
                        //Log.Print($"{this.Name}: Setting {selectedCell.Name} to {setting.ResourcePath} (a required instance)");
                        selectedCell.SetRoomGenerationResource(setting, i);

                        // Update our values
                        requiredInstances[setting] -= 1;
                        maximumInstances[setting] -= 1;
                        cellPossibilities.Remove(selectedCell.GridPosition);
                        break;
                    }
                }

                untestedCells.Remove(selectedCell.GridPosition);
            }
        }

        // Generate cells until there are no more left or we hit an unsolvable contradiction
        LayoutCell previousCell = null;
        HashSet<CellPossibility> solveContradictionBannedPossibilities = new HashSet<CellPossibility>();
        int loopCounter = -1;
        while (cellPossibilities.Count > 0)
        {
            loopCounter++;
            if (loopCounter % 10 == 0)
            {
                // Await to give control back to the game loop
                await Task.Delay(10);
            }

            // Update possibilities of all cells (update the certainty of wave functions) (here is the bottleneck)
            foreach (var kvp in cellPossibilities)
            {
                kvp.Value.Clear();
                LayoutCell selectedCell = layout.GetCell(kvp.Key);
                foreach (var setting in settingCollection.Collection)
                {
                    // Skip this row if the maximum instance limit has been reached
                    // (is not <= 0 to make -1 work as infinite)
                    if (maximumInstances[setting] == 0)
                    {
                        continue;
                    }

                    // Add every valid rotation to our possibility
                    int initialRotation = randomStream.RandiRange(0, 3);
                    for (int i = initialRotation; i < initialRotation + 4; i++)
                    {
                        bool isValid = selectedCell.IsRoomGenerationResourceValid(setting, i);
                        if (isValid)
                        {
                            CellPossibility possibility = new CellPossibility(setting, i);

                            // Do not add if this possibility is temporarily banned to solve a contradiction 
                            if (solveContradictionBannedPossibilities.Contains(possibility))
                            {
                                continue;
                            }

                            // Add to list of possibilities and break since we only need one valid rotation
                            //Log.Print($"{this.Name}: Adding {possibility} for {selectedCell.Name} to possibilities");
                            kvp.Value.Add(possibility);
                            break;
                        }
                    }
                }
            }

            // Get the cell with the least possibilities (wave function which is most certain)
            LayoutCell currentCell = null;
            foreach (var kvp in cellPossibilities)
            {
                if (currentCell == null || kvp.Value.Count < cellPossibilities[currentCell.GridPosition].Count)
                {
                    currentCell = layout.GetCell(kvp.Key);
                }
            }

            // Reset this cell when its blocked by another cell and go to the next iteration
            if (currentCell.IsBlockedByNeighbour())
            {
               //Log.Print($"{this.Name}: Acknowledged {currentCell.Name} being disabled by a neighbour");
                currentCell.SetRoomGenerationResource(null, 0);
                cellPossibilities.Remove(currentCell.GridPosition);
                continue;
            }

            // Skip this cell if its already generated (however that happened)
            if (currentCell.IsGenerated)
            {
                //Log.Print($"{this.Name}: Skipping {currentCell.Name} since it is already generated");
                cellPossibilities.Remove(currentCell.GridPosition);
                continue;
            }

            // If we find a cell with no possible rows we've hit a contradiction,
            // try to redo the last setted cell to solve it OR abort if it's unsolvable...
            if (cellPossibilities[currentCell.GridPosition].Count <= 0)
            {
                if (solveContradictionBannedPossibilities.Count <= settingCollection.Collection.Count * 4 
                    && previousCell is { RoomGenerationResource: not null })
                {
                    var bannedPossibility = new CellPossibility(previousCell.RoomGenerationResource,
                        previousCell.GridRotation);
                    Log.Print(
                        $"{this.Name}: Redoing {previousCell.Name} to solve a contradiction for {currentCell.Name} (temporarily banned possibility {bannedPossibility})");
					solveContradictionBannedPossibilities.Add(bannedPossibility);
					cellPossibilities.TryAdd(previousCell.GridPosition, new List<CellPossibility>());
                    previousCell.ResetRoomGenerationResource();
                    continue;
                }

                Log.PrintWarn(
                    $"{this.Name}: Could not solve contradiction for {currentCell.Name}. Aborting...");
                return ELayoutGenerationResult.SolvingContradictionFailed;
            }

            // Reset banned possibilities if there are any left, because at this we solved the contradiction
            if (currentCell != previousCell && solveContradictionBannedPossibilities.Count > 0)
            {
                Log.Print(
                    $"{this.Name}: Contradiction solved, resetting {solveContradictionBannedPossibilities.Count} temporarily banned possibilities");
                solveContradictionBannedPossibilities.Clear();
            }

            // Set cell to a possible row (collapse wave function)
            List<CellPossibility> weightedCellPossibilities = new List<CellPossibility>();
            foreach (var possibility in cellPossibilities[currentCell.GridPosition])
            {
                for (int i = 0; i < possibility.RoomGenerationResource.SpawnPoolEntries; i++)
                {
                    weightedCellPossibilities.Add(possibility);
                }
            }

            var selectedPossibility = 
                weightedCellPossibilities[randomStream.RandiRange(0, weightedCellPossibilities.Count - 1)];
            //Log.Print($"{this.Name}: Setting {currentCell.Name} to {selectedPossibility}!");
            currentCell.SetUniqueCellSeed(randomStream.Randi());
            currentCell.SetRoomGenerationResource(selectedPossibility.RoomGenerationResource, selectedPossibility.Rotation);

            // Update maximum instance variable
            maximumInstances[selectedPossibility.RoomGenerationResource] -= 1;

            // Remove from possibilities
            cellPossibilities.Remove(currentCell.GridPosition);
            previousCell = currentCell;
        }

        // Run post spawn validation
        // (which I can't do because at this point idk what room was set by which generation setting aaaaaaa)
        for (int x = 0; x < layout.GridSize.X; x++)
        {
            for (int y = 0; y < layout.GridSize.Y; y++)
            {
                LayoutCell currentCell = layout.GetCell(new Vector2I(x, y));
                if (currentCell.RoomGenerationResource == null) continue;

                foreach (SpawnValidator validator in currentCell.RoomGenerationResource.PostSpawnValidatorResources)
                {
                    bool isValid = validator.IsSpawnValid(layout, currentCell);
                    if (!isValid)
                    {
                        Log.PrintWarn(
                            $"{this.Name}: Post spawn validator {validator.ResourcePath} returned false for {currentCell.Name}. Aborting...");
                        return ELayoutGenerationResult.PostSpawnValidationFailed;
                    }
                }
            }
        }

        ulong endTime = Time.GetTicksMsec();
        Log.Print(
            $"{this.Name}: Finished layout generation, took {(double)(endTime - startTime) / 1000} seconds");
        return ELayoutGenerationResult.Success;
    }
}
