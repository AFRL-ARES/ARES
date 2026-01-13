using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Antlr4CodeCompletion.CoreUnitTest.Utils
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Port of antlr-c3 java unit test library to c#
    /// The c3 engine is able to provide code completion candidates useful for
    /// editors with ANTLR generated parsers, independent of the actual
    /// language/grammar used for the generation.
    /// https://github.com/mike-lischke/antlr4-c3
    /// </remarks>
    public class CountingErrorListener : BaseErrorListener
    {
        public int ErrorCount { get; private set; } = 0;
        
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            base.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
            this.ErrorCount++;
        }
    }
}
