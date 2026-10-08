namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public enum AccessRightsType : short
	{
		/// <summary>
		/// Publicly accessible by everyone. Usage note: Permissible obstacles include
		/// registration and request for API keys, as long as anyone can request such
		/// registration and/or API keys.
		/// </summary>
		Public,

		/// <summary>
		/// Only available under certain conditions. Usage note: This category may include
		/// resources that require payment, resources shared under non-disclosure
		/// agreements, resources for which the publisher or owner has not yet decided if
		/// they can be publicly released.
		/// </summary>
		Restricted,
		
		/// <summary>
		/// Not publicly accessible for privacy, security or other reasons. Usage note:
		/// This category may include resources that contain sensitive or personal
		/// information.
		/// </summary>
		NonPublic,
		
		/// <summary>
		/// Sensitive non-classified (SNC) information, information whose unauthorised
		/// disclosure could cause damage to the Commission or other interested parties
		/// such as businesses, companies, intellectual property or personal data but which
		/// is not EU classified information.
		/// </summary>
		Sensitive,
		
		/// <summary>
		/// Information that is not disclosed. Usage note: Disclosure of this data could
		/// cause damage to interested parties such as public administrations and
		/// businesses. It may refer to personal and professional information as well as to
		/// information in the context of business, commerce or trade.
		/// </summary>
		Confidential,
		Provisional
	}
}