using System;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace qwb_to_sqe
{
	public class QwbWord : Word
	{
		// Query strings for analysing a word from QWB
		private static readonly Regex NormalSignsRegex =
				new Regex("[+\u0591-\u05BD\u05BF-\u05EA\u05F3 ╱∵Α-ω⋺0-9]");

		private static readonly Regex ConjectureRegex = new Regex("\\^!\\^");

		private static readonly Regex DeletedRegex = new Regex("(\\{\\{)|(\\}\\})");

		private static readonly Regex IgnoreRegex =
				new Regex("([.])|(\\(\\))|(\\[[0-9]+\\])|(\\?\\?\\?)");

		private static readonly Regex OriginalRegex = new Regex("[⟦⟧]");

		private static readonly Regex InsertedRegex = new Regex("(\\+≪)|(≫\\+)");

		private static readonly Regex CorrectedRegex = new Regex("[≪«≫]");
		private static readonly Regex SubScriptRegex = new Regex("##");

		private static readonly Regex LineNumberRegex = new Regex("^ *\\[[0-9]\\] *$");

		// Holds the previous processed word
		private static QwbWord _previousWord;

		public static MySqlConnection conn = new MySqlConnection(
				"server=localhost;user=root" + ";database=SQE;" + "port=33067;" + "password=none");

		private Sign _postSign;

		private Sign _precedingSign;

		public string Book;
		public string Fragment;
		public string Line;

		public int qwbBookId;
		public int qwbBookPosition;
		public int SqeFragmentId;
		public int SQELineId;

		public SqeManuscript SqeManuscript;

		public string Word;
		public int    EditionId => SqeManuscript.EditionId;
		public int    EditorId  => SqeManuscript.EditorId;

		public void ProcessWord()
		{
			ProcessReference();
			ProcessWordAsQwbWord();

			// Finally, set this word as previous word
			_previousWord = this;
		}

		protected string normalize(string word)
		{
			if (LineNumberRegex.IsMatch(word))
				return "";

			word = ConjectureRegex.Replace(word, "!");
			word = DeletedRegex.Replace(word, "∆");
			word = IgnoreRegex.Replace(word, "");
			word = SubScriptRegex.Replace(word, "∇");
			word = OriginalRegex.Replace(word, "∰");
			word = InsertedRegex.Replace(word, "⊤");
			word = CorrectedRegex.Replace(word, "∭");

			return word;
		}

		private void ProcessWordAsQwbWord()
		{
			QwbSign currSign = null;

			// TODO - Is this needed?
			if (Word == "-")
			{
				//_isDummy = true;
			}
			else
			{
				var normalizedWord = normalize(Word);

				foreach (var signChar in normalizedWord.ToCharArray())
				{
					if (NormalSignsRegex.IsMatch(signChar.ToString()))
						currSign = addSign(signChar);
					else
					{
						switch (signChar)
						{
							case '\u05AF': // Circellus
								currSign.HasCirc = true;

								break;

							case '\u05BE': // Single destroyed sign
								if (currSign == null)
									currSign = addSign(' ');

								currSign.expandDestroyedSign();

								break;

							case '_': // Vacat
								if (currSign == null)
									currSign = addSign(' ');

								currSign.expandVacat();

								break;

							case '-': // Destroyed area
								if (currSign == null)
									currSign = addSign(' ');

								currSign.expandDestroyedRegion();

								break;

							case '^': // Switch for superscript
								isSuperScript = !isSuperScript;

								break;

							case '∇': // Switch for subscript
								IsSubScript = !IsSubScript;

								break;

							case '!': // Switch for conjecture
								IsConjecture = !IsConjecture;

								break;

							case '[': // Start of reconstructed
								IsReconstructed = true;

								break;

							case ']': // End of reconstructed
								IsReconstructed = false;

								break;

							case '{': // Start of wrongly written additional text
								WrongAddition = true;

								break;

							case '}': // End of wrongly written additional text
								WrongAddition = false;

								break;

							case '∆': // Switch of erased text
								IsDeleted = !IsDeleted;

								break;

							case '<': // Start of forgotten text
								Forgotten = true;

								break;

							case '>': // End of forgotten text
								Forgotten = false;

								break;

							case '‹': // Start of forgotten text
								Forgotten = true;

								break;

							case '›': // End of forgotten text
								Forgotten = false;

								break;

							case '/': // The following sign is a variant reading
								IsVariant = currSign.is_variant + 1;

								break;

							case '∰': // An original sign later corrected into new sign(s)
								IsOriginal = !IsOriginal;

								break;

							case '∭': // A sign corrected from a different sign
								IsCorrected = !IsCorrected;

								break;

							case '⊤': // A sign added later
								IsInserted = !IsInserted;

								break;

							case '@':
								IsMarginVariant = !IsMarginVariant;

								break;

							case '(':
								IsQuestionable = true;

								break;

							case ')':
								IsQuestionable = false;

								break;

							case '?':
								if (currSign != null)
									currSign.IsQuestionable = true;

								break;

							case '|':
								currSign = new QwbSign("", 10, 11);
								signs.Add(currSign);

								break;

							case '┓':
								currSign = addSign('┓', 7);

								break;

							default:
								Console.WriteLine(
										signChar
										+ "="
										+ qwbId
										+ ": "
										+ Word
										+ " = '"
										+ normalizedWord
										+ "'");

								break;
						}
					}
				}
			}
		}

		protected QwbSign addSign(char sign, params int[] attributeValues)
		{
			var qwbSign = new QwbSign(sign.ToString(), attributeValues)
			{
					IsSuperScript = isSuperScript
					, IsReconstructed = IsReconstructed
					, IsWrongAddition = WrongAddition
					, IsForgotten = Forgotten
					, IsSubScript = IsSubScript
					, is_variant = IsVariant
					, IsOriginal = IsOriginal
					, IsCorrected = IsCorrected
					, IsMarginVariant = IsMarginVariant
					, IsQuestionable = IsQuestionable
					, IsConjecture = IsConjecture
					, IsInserted = IsInserted
					,
			};

			IsVariant = 0;
			signs.Add(qwbSign);

			return qwbSign;
		}

		public void print()
		{
			Console.WriteLine($"{Book} ({qwbBookId}) {Fragment},{Line} ({qwbBookPosition})\n");

			foreach (var sign in signs)
			{
				Console.Write($"\t{sign.character} => ");

				foreach (var attribute in sign.Attributes.Values)
					Console.Write($"{attribute.attribute_value_id}={attribute.value}, ");

				Console.WriteLine("");
			}

			Console.WriteLine("");
		}

		private void ProcessReference()
		{
			// Change the Fragment name to the version used in SQE
			if (!Fragment.StartsWith("fr"))
				Fragment = "col. " + Fragment;

			if (_previousWord?.Book != Book)
			{
				//If no _currBook has been set, than we are at the start and we need only to set Scroll,Frag, and Linestart
				//otherwise we have to add also Scroll-, Frag-, and Line-end
				if (_previousWord?.Book != "")
				{
					_previousWord._postSign = new QwbSign(
							""
							, 15
							, 13
							, 11);
				}

				_precedingSign = new QwbSign(
						""
						, 14
						, 12
						, 10);

				//            SqeManuscript = SqeDatabase.GetSqeManuscript(Book);

				Fragment = "";
				Console.Write($"\n{Book}");
			}
			else if (_previousWord.Fragment != Fragment)
			{
				if (_precedingSign == null)
				{
					_previousWord._postSign = new QwbSign("", 11, 13);
					_precedingSign = new QwbSign("", 12, 10);
				}

				//             SqeFragmentId = SqeDatabase.GetSqeFragmentId(Fragment, _previousWord.SqeFragmentId);

				Console.Write($"\n\t{Fragment},");
			}
			else if (_previousWord.Line != Line)
			{
				_precedingSign.Add(new QwbSign("", 11));
				_precedingSign.Add(new QwbSign("", 10));

				Console.Write($"{Line}.");
			}
			else
			{
				var preSign = new QwbSign("");

				preSign.AddAttribute(2, 1);
				_precedingSign.Add(preSign);
			}
		}
	}
}
