using System.Collections.Generic;
using Unity.Android.Types;
using static Utils;


public class Context {
    public List<Symbol> symbols;
    public List<string> lines;
    public int remaining;

    public string DefineVariable(StrictType type, string name, string value, bool constant = false) {
        string newName = name + "???";
        string suffix = constant ? "const " : "";
        lines.Add(suffix + type.ToStringType() + " " + newName + " = " + value + ";");
        return newName;
    }
}

public abstract class Symbol {
    public string name;
    public StrictType type;

    // simple pre-pass that flatten the whole tree
    public virtual void PrePass(Context context) { }

    // converts the symbol to source code
    public abstract void Parse(Context ctx);
}

public class DefineConst : Symbol {
    public string value;

    public override void Parse(Context ctx) {
        this.name = ctx.DefineVariable(this.type, "???", value, true);
    }
}

public class Add : Symbol {
    public Symbol a, b;

    public override void Parse(Context ctx) {
        // we are 100% confident that a.name and b.name have been set since they have been already parsed
        this.name = ctx.DefineVariable(a.type, $"{a.name}_add_{b.name}", $"{a.name} + {b.name}");
    }

    public override void PrePass(Context context) {
        context.symbols.Add(a);
        context.symbols.Add(b);
        context.remaining += 2;
    }
}



public class Variable<T> {
    private Symbol symbol;

    public static implicit operator Variable<T>(T value) {
        return new Variable<T>() {
            symbol = new DefineConst { value = value.ToString() },
        };
    }

    public static Variable<T> operator +(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new Add { a = a.symbol, b = b.symbol },
        };
    }
}



public static class Parser {
    /*
    public static void ParseOne(Symbol val) {
        switch (val.op) {
            case Operation.Define:
                string a = Utils.TypeOf<T>().ToStringType() + " " + newName + " = " + value + ";");
                // call code gen to define
                break;
            case Operation.DefineConst:
                // call code gen to define const
                break;
            case Operation.Swizzle:
                // call code gen to do swizzle stuff
                break;
            case Operation.Add:
                // call code gen to sum list[0] and list[1]
                break;
            case Operation.Sub:
                // call code gen to sub list[0] by list[1]
                break;
            case Operation.Mul:
                // call code gen to mul list[0] by list[1]
                break;
            case Operation.Div:
                // add lines for div list[0] by list[1]
                break;
        }
    }
    */

    public static void Parse(Symbol head) {
        Context ctx = new Context {
            symbols = new List<Symbol>(),
            lines = new List<string>(),
        };

        ctx.symbols.Add(head);
        ctx.remaining = 1;

        // forward pass, add all symbols to list
        for (int i = 0; i < 5000; i++) {
            if (ctx.remaining == 0) {
                break;
            }

            ctx.symbols[i].PrePass(ctx);
        }

        // backward pass, parse the symbols
        for (int i = ctx.symbols.Count - 1; i >= 0; i--) {
            ctx.symbols[i].Parse(ctx);
        }
    }
}



/*

var a = 0.25f;
var b = position.y;
var c = a + b;
density = c

// density: assin to c -> c: a op_addition b, a: const float 0.25, b: position decompose y
// 

density => (a,b)
(a) => ()



*/