using System;
using System.Collections.Generic;

namespace AI.Image.ImageSets;

public class ImageExportInput
{
    /// <summary>指定图片 ID 列表</summary>
    public List<Guid>? Ids { get; set; }

    /// <summary>如果 Ids 为空，则按项目 ID 导出全部</summary>
    public Guid? ProjectId { get; set; }
}
