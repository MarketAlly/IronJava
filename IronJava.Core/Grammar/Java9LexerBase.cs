/*
 * [The "BSD license"]
 *  Copyright (c) 2014 Terence Parr
 *  Copyright (c) 2014 Sam Harwell
 *  Copyright (c) 2017 Chan Chung Kwong
 *  All rights reserved.
 *
 *  Redistribution and use in source and binary forms, with or without
 *  modification, are permitted provided that the following conditions
 *  are met:
 *
 *  1. Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *  2. Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in the
 *     documentation and/or other materials provided with the distribution.
 *  3. The name of the author may not be used to endorse or promote products
 *     derived from this software without specific prior written permission.
 *
 *  THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 *  IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 *  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 *  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 *  INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 *  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 *  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 *  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 *  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 *  THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using Antlr4.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarketAlly.IronJava.Core.Grammar
{
    public abstract class Java9LexerBase : Lexer
{
    private readonly ICharStream _input;

    protected Java9LexerBase(ICharStream input, TextWriter output, TextWriter errorOutput)
        : base(input, output, errorOutput) {
            _input = input;
    }

    private class Character
    {
        public static bool isJavaIdentifierPart(int c)
        {
            if (Char.IsLetter((char)c))
                return true;
            else if (c == (int)'$')
                return true;
            else if (c == (int)'_')
                return true;
            else if (Char.IsDigit((char)c))
                return true;
            else if (Char.IsNumber((char)c))
                return true;
            return false;
        }

        public static bool isJavaIdentifierStart(int c)
        {
            if (Char.IsLetter((char)c))
                return true;
            else if (c == (int)'$')
                return true;
            else if (c == (int)'_')
                return true;
            return false;
        }

        public static int toCodePoint(int high, int low)
        {
            return Char.ConvertToUtf32((char)high, (char)low);
        }
    }

    public bool Check1()
    {
        return Character.isJavaIdentifierStart(_input.LA(-1));
    }

    public bool Check2()
    {
        return Character.isJavaIdentifierStart(Character.toCodePoint((char)_input.LA(-2), (char)_input.LA(-1)));
    }

    public bool Check3()
    {
        return Character.isJavaIdentifierPart(_input.LA(-1));
    }

    public bool Check4()
    {
        return Character.isJavaIdentifierPart(Character.toCodePoint((char)_input.LA(-2), (char)_input.LA(-1)));
    }
}
}
