using System;

namespace mAdcOW.SharePoint.KqlParser
{
    [Flags]
    public enum TokenType
    {
        Operator = 1, Phrase = 2, Group = 4, Word = 8, Property = 16
    }
}