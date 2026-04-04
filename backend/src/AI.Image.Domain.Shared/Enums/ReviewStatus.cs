namespace AI.Image.Enums;

/// <summary>
/// 图片评审状态
/// </summary>
public enum ReviewStatus
{
    /// <summary>待评审</summary>
    Pending = 0,
    /// <summary>已选中</summary>
    Selected = 1,
    /// <summary>已淘汰</summary>
    Rejected = 2
}
