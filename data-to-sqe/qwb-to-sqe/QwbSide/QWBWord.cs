using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace qwb_to_sqe
{
    public class QWBWord
    {
        private static readonly List<uint> AttributeIds = new List<uint>();

        public bool ContainsLineBreak = false;


        private const uint Destroyed = 5;
        private const uint Vacat = 4;
        private const uint SuperScript = 30;
        private const uint SubScript = 31;
        private const uint Reconstructed = 20;
        private const uint Deleted = 33;
        private const uint WrongAddition = 23;
        private const uint Forgotten = 22;
        private const uint Original = 41;
        private const uint Inserted = 43;
        private const uint Corrected = 42;
        private const uint MarginVariant = 38;
        private const uint Questionable = 44;
        private const uint Conjecture = 21;
        private const uint Circellus = 19;
        private int IsVariant = 0;


        public uint QWBWordId;
        public uint SQEWordId;

        private string _qwbText;

        public string QWBText
        {
            get => _qwbText;
            set => _qwbText = _processQWBWord(value);
        }

        private readonly List<SignData> _signs = new List<SignData>();




        // Query strings for analysing a word from QWB
        private static readonly Regex NormalSignsRegex = new Regex("[+\u0591-\u05BD\u05BF-\u05EA\u05F3 ╱∵Α-ω⋺0-9]");
        private static readonly Regex ConjectureRegex = new Regex("\\^!\\^");
        private static readonly Regex DeletedRegex = new Regex("(\\{\\{)|(\\}\\})");
        private static readonly Regex IgnoreRegex = new Regex("([.])|(\\(\\))|(\\[[0-9]+\\])|(\\?\\?\\?)");
        private static readonly Regex OriginalRegex = new Regex("[⟦⟧]");
        private static readonly Regex InsertedRegex = new Regex("(\\+≪)|(≫\\+)");
        private static readonly Regex CorrectedRegex = new Regex("[≪«≫]");
        private static readonly Regex SubScriptRegex = new Regex("##");
        private static readonly Regex LineNumberRegex = new Regex("^ *\\[[0-9]\\] *$");

        private string _processQWBWord(string qwbWord)
        {
            var word = _normalize(qwbWord);
            SignData currSign = null;
            foreach (var signChar in word.ToCharArray())
                if (NormalSignsRegex.IsMatch(signChar.ToString()))
                    currSign = _newSign(signChar);
                else
                    switch (signChar)
                    {
                        case '\u05AF': // Circellus
                            _addAttribute(currSign, Circellus);
                            break;
                        case '\u05BE': // Single destroyed sign
                            if (_lastAttributeIs(currSign, Destroyed) == false) currSign = _newSign(' ', Destroyed, 0);
                            _expandLastNumericValue(currSign, Destroyed);
                            break;
                        case '_': // Vacat
                            if (_lastAttributeIs(currSign, Vacat) == false)
                            {
                                currSign = SignFactory.CreateVacatSign(0);
                                _handleNewSign(currSign);
                            }
                            _expandLastNumericValue(currSign, Vacat);
                            break;
                        case '-': // Destroyed area
                            if (_lastAttributeIs(currSign, Destroyed) == false)
                            {
                                currSign = SignFactory.CreateDamageSign(0);
                                _handleNewSign(currSign);
                            }
                            _expandLastNumericValue(currSign, Destroyed, 3);
                            break;
                        case '^': // Switch for superscript
                            _switchAttribute(SuperScript);
                            break;
                        case '∇': // Switch for subscript
                            _switchAttribute(SubScript);
                            break;
                        case '!': // Switch for conjecture
                            _switchAttribute(Conjecture);
                            break;
                        case '[': // Start of reconstructed
                            AttributeIds.Add(Reconstructed);
                            break;
                        case ']': // End of reconstructed
                            AttributeIds.RemoveAll(id => id == Reconstructed);
                            break;
                        case '{': // Start of wrongly written additional text
                            AttributeIds.Add(WrongAddition);
                            break;
                        case '}': // End of wrongly written additional text
                            AttributeIds.RemoveAll(id => id == WrongAddition);
                            break;
                        case '∆': // Switch of erased text
                            _switchAttribute(Deleted);
                            break;
                        case '<': // Start of forgotten text
                            AttributeIds.Add(Forgotten);
                            break;
                        case '>': // End of forgotten text
                            AttributeIds.RemoveAll(id => id == Forgotten);
                            break;
                        case '‹': // Start of forgotten text
                            AttributeIds.Add(Forgotten);
                            break;
                        case '›': // End of forgotten text
                            AttributeIds.RemoveAll(id => id == Forgotten);
                            break;
                        case '/': // The following sign is a variant reading
                            currSign.SignInterpretations.Add(
                                SignInterpretationFactory.CreateCharacterInterpretation(signChar.ToString()));
                            break;
                        case '∰': // An original sign later corrected into new sign(s)
                            _switchAttribute(Original);
                            break;
                        case '∭': // A sign corrected from a different sign
                            _switchAttribute(Corrected);
                            break;
                        case '⊤': // A sign added later
                            _switchAttribute(Inserted);
                            break;
                        case '@':
                            _switchAttribute(MarginVariant);
                            break;
                        case '(':
                            AttributeIds.Add(Questionable);
                            break;
                        case ')':
                            AttributeIds.RemoveAll(id => id == Questionable);
                            break;
                        case '?':
                            _addAttribute(currSign, Questionable);
                            break;
                        case '|':
                            currSign = SignFactory.CreateTerminatorSign(TableData.Table.line,
                                TableData.TerminatorType.End);
                            currSign =  SignFactory.CreateTerminatorSign(TableData.Table.line,
                                TableData.TerminatorType.Start);
                            ContainsLineBreak = true;
                            break;
                        case '┓':
                            currSign = _newSign('┓', 7);
                            break;
                        default:
                            Console.WriteLine(signChar + "=" + QWBWordId + ": " + qwbWord + " = '" + word + "'");
                            break;
                    }


            return qwbWord;
        }

        private string _normalize(string word)
        {
            if (LineNumberRegex.IsMatch(word)) return "";
            word = ConjectureRegex.Replace(word, "!");
            word = DeletedRegex.Replace(word, "∆");
            word = IgnoreRegex.Replace(word, "");
            word = SubScriptRegex.Replace(word, "∇");
            word = OriginalRegex.Replace(word, "∰");
            word = InsertedRegex.Replace(word, "⊤");
            word = CorrectedRegex.Replace(word, "∭");

            return word;
        }

        private SignData _newSign(char signChar, uint? additionalAttributeId = null, uint? numericValue = null)
        {
            var newSign = SignFactory.CreateSimpleCharacterSign(signChar.ToString());
            _handleNewSign(newSign, additionalAttributeId, numericValue);
            return newSign;
        }

        private void _handleNewSign(SignData sign, uint? additionalAttributeId = null, uint? numericValue = null)
        {
            _signs.Add(sign);
            if (additionalAttributeId != null) _addAttribute(sign, additionalAttributeId.Value, numericValue);
            foreach (var id in AttributeIds.Distinct())
            {
                _addAttribute(sign, id);
            }

        }

        private void _switchAttribute(uint id)
        {
            if (AttributeIds.Contains(id)) AttributeIds.RemoveAll(existingId => existingId == id);
            else AttributeIds.Add(id);
        }

        private void _addAttribute(SignData sign, uint attributeId, uint? numericValue = null)
        {
            sign?.SignInterpretations.Last().Attributes.Add(new SignInterpretationAttributeData()
            {
                AttributeValueId = attributeId,
                NumericValue = numericValue
            });
        }

        private bool _lastAttributeIs(SignData sign, uint attributeId)
        {
            return (sign?.SignInterpretations.Last().Attributes.Exists(data => data.AttributeValueId == attributeId)) ?? false;
        }

        private void _expandLastNumericValue(SignData sign, uint attributeValueId, uint by = 1)
        {
            sign.SignInterpretations.Last().Attributes.Find(attr =>
                attr.AttributeValueId == attributeValueId).NumericValue += by;
        }



    
}
}