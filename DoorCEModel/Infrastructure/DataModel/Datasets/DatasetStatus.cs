namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public enum DatasetStatus : short
	{
		/// <summary>
		/// ready for publication - waiting for approval
		/// </summary>
		Draft,

		/// <summary>
		/// publicly available (published)
		/// </summary>
		Active,

		/// <summary>
		/// stopped being publicly available
		/// </summary>
		Deleted,

		/// <summary>
		/// non-published working version, after or in the process of acquisition
		/// </summary>
		Source,
		
		/// <summary>
		/// publicly available (published) with structured data managed independently of UDAS
		/// </summary>
		Independent
	}
}