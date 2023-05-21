using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static RSDKv4.Script;

namespace RSDKv4.Bytecode;

public class BytecodeTranslator
{
    private VAR[] variables;
    private FUNC[] functions;

    public BytecodeTranslator(EngineRevision revision)
    {
        switch (revision)
        {
            case EngineRevision.Rev0:
                variables = BytecodeRev0.variables;
                functions = BytecodeRev0.functions;
                break;
            case EngineRevision.Rev1:
                variables = BytecodeRev1.variables;
                functions = BytecodeRev1.functions;
                break;
            case EngineRevision.Rev2:
                variables = BytecodeRev2.variables;
                functions = BytecodeRev2.functions;
                break;
            case EngineRevision.Rev3:
                variables = BytecodeRev3.variables;
                functions = BytecodeRev3.functions;
                break;
            default:
                break;
        }
    }

    public void TranslateVariable(int idx, out VAR variable)
    {
        variable = variables[idx];
    }

    public void TranslateFunction(int idx, out FUNC func)
    {
        func = functions[idx];
    }
}
