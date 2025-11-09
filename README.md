# Placement System

A high-performance object placement system for Unity using Unity Jobs and Burst compilation for efficient spatial queries and obstacle avoidance.

## Features

- **High Performance**: Utilizes Unity Jobs and Burst compilation for maximum efficiency
- **Parallel Processing**: True multi-threaded processing using IJobParallelFor for optimal CPU utilization
- **Asynchronous Processing**: Non-blocking inflation calculations spread across multiple frames
- **Spatial Queries**: Fast placement validation and nearest available position finding
- **Dynamic Obstacles**: Add and remove obstacles at runtime with automatic inflation updates
- **Configurable**: Adjustable inflation radius and frame budget for performance tuning

## Algorithms

This package implements several well-known algorithms for efficient spatial queries:

- **[Distance Transform](https://en.wikipedia.org/wiki/Distance_transform)**: Creates a distance field from obstacles, allowing O(1) placement validation queries
- **[Breadth-First Search (BFS)](https://en.wikipedia.org/wiki/Breadth-first_search)**: Used for propagating the distance field from obstacles outward using a frontier-based approach
- **[Euclidean Distance Transform](https://en.wikipedia.org/wiki/Distance_transform#Euclidean_distance_transform)**: Calculates accurate Euclidean distances in the grid, supporting both 4-connected and 8-connected neighbor traversal
- **[Grid-Based Spatial Partitioning](https://en.wikipedia.org/wiki/Space_partitioning)**: Uses a uniform grid data structure for efficient spatial queries and obstacle management

The system combines these algorithms with Unity's Job System and Burst compilation to achieve high-performance spatial queries suitable for real-time games.

## Requirements

- Unity 2022.3 or later
- Unity Collections
- Unity Mathematics
- Unity Burst
- Unity Jobs

## Installation

### Via Unity Package Manager (Git URL)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the **+** button
3. Select **Add package from git URL...**
4. Enter: `https://github.com/Diamoundz/PlacementSystem.git`

## Quick Start

```csharp
using AengelStudio.PlacementSystem;
using UnityEngine;

public class PlacementExample : MonoBehaviour
{
    private PlacementAPI placementAPI;

    void Start()
    {
        // Create a placement system (256x256 grid, 64 unit inflation radius, 2ms budget per frame)
        placementAPI = new PlacementAPI(256, 256, 64f, 2f);
    }

    void Update()
    {
        // Add obstacle on right-click
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.FloorToInt(mousePos.x);
            int y = Mathf.FloorToInt(mousePos.y);
            placementAPI.AddObstacle(x, y, 5, this);
        }

        // Check placement on left-click
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.FloorToInt(mousePos.x);
            int y = Mathf.FloorToInt(mousePos.y);
            
            if (placementAPI.CanPlaceObject(x, y, 5f))
            {
                Debug.Log("Can place here!");
            }
            else
            {
                var closest = placementAPI.FindClosestAvailablePosition(x, y, 5f);
                if (closest.HasValue)
                {
                    Debug.Log($"Closest available: {closest.Value}");
                }
            }
        }
    }

    void OnDestroy()
    {
        placementAPI?.Dispose();
    }
}
```

## API Reference

### PlacementAPI

Main entry point for the placement system.

#### Constructor
```csharp
PlacementAPI(int width, int height, float inflationRadius, float inflationBudgetMs)
```

#### Methods

- `AddObstacle(int x, int y, int radius, MonoBehaviour coroutineHost, Action onComplete = null)`: Adds an obstacle and starts asynchronous inflation
- `CanPlaceObject(int x, int y, float radius)`: Returns true if placement is valid
- `FindClosestAvailablePosition(int x, int y, float radius)`: Returns the nearest available position
- `CompleteInflationNow()`: Forces synchronous completion of inflation
- `Dispose()`: Cleans up native resources

## Performance Tips

- Adjust `inflationBudgetMs` to balance frame time vs. inflation speed
- Use smaller inflation radius for better performance
- Call `CompleteInflationNow()` when you need immediate results
- Dispose of PlacementAPI when no longer needed to free native memory

## License

MIT License - See [LICENSE](LICENSE) file for details

## Support

For issues, questions, or contributions, please visit [https://github.com/Diamoundz/PlacementSystem](https://github.com/Diamoundz/PlacementSystem)

