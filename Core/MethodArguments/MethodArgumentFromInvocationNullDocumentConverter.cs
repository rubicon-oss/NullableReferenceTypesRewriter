﻿// Copyright (c) rubicon IT GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypesRewriter.MethodArguments
{
  ///<summary>
  /// A DocumentConverter, which annotates arguments of a method as nullable, if the method is called with <see langword="null" /> parameters.
  /// Due to the implementation of DocumentConverters it is currently not possible to track method calls across documents (classes).
  /// <see cref="IDocumentConverter"/>
  /// </summary>
  public class MethodArgumentFromInvocationNullDocumentConverter : IDocumentConverter
  {
    public async Task<Document> Convert (Document document)
    {
      var semantic = await document.GetSemanticModelAsync()
                     ?? throw new ArgumentException ($"Document '{document.FilePath}' does not support providing a semantic model.");
      var syntax = await document.GetSyntaxRootAsync()
                   ?? throw new ArgumentException ($"Document '{document.FilePath}' does not support providing a syntax tree.");

      var argList = new HashSet<ParameterSyntax>();

      var _ = new MethodArgumentFromInvocationNullAnnotator (semantic, arg => argList.Add (arg)).Visit (syntax);
      var newSyntax = new MethodParameterNullAnnotator (argList).Visit (syntax);

      return document.WithSyntaxRoot (newSyntax);
    }
  }
}