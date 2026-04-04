using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace AI.Image.ImageSets;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class ImageItemMapper : TwoWayMapperBase<ImageItem, ImageItemDto>
{
    public override partial ImageItemDto Map(ImageItem source);
    public override partial void Map(ImageItem source, ImageItemDto destination);
    public override partial ImageItem ReverseMap(ImageItemDto source);
    public override partial void ReverseMap(ImageItemDto source, ImageItem destination);
}
