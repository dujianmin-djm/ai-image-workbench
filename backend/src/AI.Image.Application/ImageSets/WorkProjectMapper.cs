using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace AI.Image.ImageSets;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class WorkProjectMapper : TwoWayMapperBase<WorkProject, WorkProjectDto>
{
    public override partial WorkProjectDto Map(WorkProject source);
    public override partial void Map(WorkProject source, WorkProjectDto destination);
    public override partial WorkProject ReverseMap(WorkProjectDto source);
    public override partial void ReverseMap(WorkProjectDto source, WorkProject destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class CreateUpdateWorkProjectMapper : MapperBase<CreateUpdateWorkProjectDto, WorkProject>
{
    public override partial WorkProject Map(CreateUpdateWorkProjectDto source);
    public override partial void Map(CreateUpdateWorkProjectDto source, WorkProject destination);
}
