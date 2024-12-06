using System;
using System.Collections.Generic;
using Unity.Android.Types;
using static Utils;


public class Context {
    public List<Symbol> symbols;
    public List<string> lines;
    private Dictionary<string, int> varNamesToId;
    private Dictionary<string, (Utils.StrictType, Func<object>)> injected;
    private List<string> properties;
    public int remaining;
    private int counter;
    public bool debugNames;

    public Context(bool debugNames) {
        this.symbols = new List<Symbol>();
        this.lines = new List<string>();
        this.properties = new List<string>();
        this.injected = new Dictionary<string, (StrictType, Func<object>)>();
        this.varNamesToId = new Dictionary<string, int>();
        this.debugNames = debugNames;
        this.counter = 0;
    }

    public string Inject(StrictType type, string name, Func<object> func) {
        string newName = GenId(name);
        injected.Add(newName, (type, () => func()));
        properties.Add(type.ToStringType() + " " + newName + ";");
        return newName;
    }

    public string GenId(string name) {
        int id = 0;

        if (varNamesToId.ContainsKey(name)) {
            id = ++varNamesToId[name];
        } else {
            varNamesToId.Add(name, 0);
        }

        if (debugNames) {
            return name + "_" + id.ToString();
        } else {
            return "_" + ++counter;
        }
    }

    public string DefineVariable(StrictType type, string name, string value, bool constant = false) {
        string newName = GenId(name);
        string suffix = constant ? "const " : "";
        lines.Add(suffix + type.ToStringType() + " " + newName + " = " + value + ";");
        return newName;
    }

    public List<string> Combine() {
        List<string> result = new List<string>();
        result.AddRange(properties);
        result.AddRange(lines);
        return result;
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
        this.name = ctx.DefineVariable(this.type, "c", $"{value}", true);
    }
}

public class Injected : Symbol {
    public Func<object> calback;

    public override void Parse(Context ctx) {
        this.name = ctx.Inject(type, "inj", calback);
    }
}

public class SimpleBinOp : Symbol {
    public Symbol a, b;
    public string op;

    public override void Parse(Context ctx) {
        this.type = a.type;
        this.name = ctx.DefineVariable(a.type, $"{a.name}_op_{b.name}", $"{a.name} {op} {b.name}");
    }

    public override void PrePass(Context context) {
        context.symbols.Add(a);
        context.symbols.Add(b);
        context.remaining += 2;
    }
}


[Serializable]
public class Inject<T> {
    public T x;
}

public class Variable<T> {
    public Symbol symbol;

    public static implicit operator Variable<T>(T value) {
        return new Variable<T>() {
            symbol = new DefineConst { value = value.ToString(), type = Utils.TypeOf<T>() },
        };
    }

    public static Variable<T> operator +(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new SimpleBinOp { a = a.symbol, b = b.symbol, op = "+" },
        };
    }

    public static Variable<T> operator -(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new SimpleBinOp { a = a.symbol, b = b.symbol, op = "-" },
        };
    }

    public static Variable<T> operator *(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new SimpleBinOp { a = a.symbol, b = b.symbol, op = "*" },
        };
    }
    public static Variable<T> operator /(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new SimpleBinOp { a = a.symbol, b = b.symbol, op = "/" },
        };
    }

    public static implicit operator Variable<T>(Inject<T> value) {
        return new Variable<T> {
            symbol = new Injected { calback = () => value.x },
        };
    }
}



public static class Parser {
    public static void Parse(Symbol head) {
        Context ctx = new Context(false);
        HashSet<Symbol> defined = new HashSet<Symbol>();

        ctx.symbols.Add(head);
        ctx.remaining = 1;

        // forward pass, add all symbols to list
        for (int i = 0; i < 5000; i++) {
            if (ctx.remaining == 0) {
                break;
            }

            ctx.symbols[i].PrePass(ctx);
            ctx.remaining--;
        }

        // backward pass, parse the symbols
        for (int i = ctx.symbols.Count - 1; i >= 0; i--) {
            if (defined.Add(ctx.symbols[i])) {
                ctx.symbols[i].Parse(ctx);
            }
        }

        foreach (string line in ctx.Combine()) {
            UnityEngine.Debug.Log(line);
        }
    }
}