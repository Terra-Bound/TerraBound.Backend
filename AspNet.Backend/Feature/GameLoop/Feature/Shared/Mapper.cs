using TerraBound.Core.Geo;

namespace AspNet.Backend.Feature.GameLoop.Feature.Shared;

public class EntityMapper : Dictionary<int, Arch.Core.Entity> { }
public class ChunkMapper : Dictionary<Grid, Arch.Core.Entity> { }
public class UserIdToEntityMapper : Dictionary<string, Arch.Core.Entity> { }