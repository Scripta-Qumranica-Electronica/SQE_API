namespace SQE.SqeHttpApi.DataAccess.Helpers
{
	// TODO We still have to find a final formulation.
	/// <summary>
	///     Provides the licence and copyright texts for a text
	/// </summary>
	public static class Licence
	{
		public const string licenceText =
			@"This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
To view a copy of this license, visit https://creativecommons.org/licenses/by-sa/4.0/legalcode 
or send a letter to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.";

		public static string printLicence(string copyrightHolder, string contributors)
		{
			return $@"© {copyrightHolder} 

Provided by {contributors} on the basis of a text provided by the Qumran-Wörterbuch of the Göttingen Academy of Sciences, 
which is based upon a preliminary text provided Martin Abegg.

{licenceText}";
		}
	}
}