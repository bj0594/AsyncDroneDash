using AsyncDroneDash.Project;
using Xunit;

namespace AsyncDroneDash.Tests;

public class DroneModelAndFlightTests
{
    [Fact]
    public void DroneFlight_ValidConfiguration_ShouldBeAccepted()
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 3,
            DelayMs = 0
        };

        // Act
        var exception = Record.Exception(() => drone.Validate());

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    public void DroneFlight_NegativeMaxCheckpoints_ShouldBeRejected(int maxCheckpoints)
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = maxCheckpoints,
            DelayMs = 0
        };

        // Act
        var act = () => drone.Validate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void DroneFlight_NegativeDelayMs_ShouldBeRejected(int delayMs)
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 1,
            DelayMs = delayMs
        };

        // Act
        var act = () => drone.Validate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void DroneFlight_Checkpoints_ShouldProgressFromZeroToMax(int maxCheckpoints)
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = maxCheckpoints,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> onEvent = events.Add;

        // Act
        drone.Fly(onEvent);

        // Assert
        var checkpoints = events
            .Where(e => e.Type == FlightEventType.CheckpointReached)
            .Select(e => e.Checkpoint)
            .ToList();

        Assert.Equal(
            Enumerable.Range(0, maxCheckpoints + 1),
            checkpoints);
    }

    [Fact]
    public void DroneFlight_ZeroMaxCheckpoints_ShouldReportZeroAndComplete()
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 0,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> onEvent = events.Add;

        // Act
        drone.Fly(onEvent);

        // Assert
        Assert.Collection(
            events,
            start => Assert.Equal(FlightEventType.Started, start.Type),
            checkpoint =>
            {
                Assert.Equal(FlightEventType.CheckpointReached, checkpoint.Type);
                Assert.Equal(0, checkpoint.Checkpoint);
            },
            completed => Assert.Equal(FlightEventType.Completed, completed.Type)
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DroneFlight_MissingOrBlankName_ShouldBeRejected(string? name)
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = name!,
            MaxCheckpoints = 1,
            DelayMs = 0
        };

        // Act
        var act = () => drone.Validate();

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void DroneFlight_ZeroDelay_ShouldCompleteSuccessfully()
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 1,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> onEvent = events.Add;

        // Act
        drone.Fly(onEvent);

        // Assert
        Assert.NotEmpty(events);
        Assert.Equal(FlightEventType.Started, events.First().Type);
        Assert.Equal(FlightEventType.Completed, events.Last().Type);
    }

    [Fact]
    public void DroneFlight_ShouldReportLifecycle()
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 3,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> onEvent = events.Add;

        // Act
        drone.Fly(onEvent);

        // Assert
        Assert.Equal(6, events.Count);

        Assert.Equal(FlightEventType.Started, events[0].Type);

        Assert.Equal(FlightEventType.CheckpointReached, events[1].Type);
        Assert.Equal(FlightEventType.CheckpointReached, events[2].Type);
        Assert.Equal(FlightEventType.CheckpointReached, events[3].Type);
        Assert.Equal(FlightEventType.CheckpointReached, events[4].Type);

        Assert.Equal(FlightEventType.Completed, events[5].Type);

        Assert.Equal(0, events[1].Checkpoint);
        Assert.Equal(1, events[2].Checkpoint);
        Assert.Equal(2, events[3].Checkpoint);
        Assert.Equal(3, events[4].Checkpoint);

        Assert.All(events, flightEvent =>
            Assert.Equal("Alpha", flightEvent.DroneName));
    }
}