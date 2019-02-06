using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctionsAnalyzer.Models
{
    class FunctionCall
    {
        public string name { get; set; }
        public SyntaxNode node { get; set; }
        public SyntaxNode parameterNode { get; set; }
        public String parameterType { get; set; }
    }
}
