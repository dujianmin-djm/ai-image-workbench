namespace AI.Image;

public static class DomainConsts
{
	public const string DefaultDbTablePrefix = "T_";
	public const string? DbSchema = null;

	public static class DbTablePrefix
	{
		public const string System = DefaultDbTablePrefix + "SYS_";
		public const string BaseData = DefaultDbTablePrefix + "BD_";
		public const string Business = DefaultDbTablePrefix + "BIZ_";
	}
}
