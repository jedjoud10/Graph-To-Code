public class Symbol {
    public Operation op;
    public object constValue;
    public string name;
    public string swizzle;
    public Symbol[] list;
}

public class Variable<T>: Symbol {
    public static implicit operator Variable<T>(T value) {
        return new Variable<T>() {
            op = Operation.DefineConst,
            name = "constant!!",
            constValue = value,
        };
    }

    public static Variable<T> operator +(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            op = Operation.Add,
            name = "added",
            constValue = default(T),
            list = new[] { a, b },
        };
    }
}

public static class Parser {
    public static void ParseOne(Symbol val) {
        switch (val.op) {
            case Operation.Define:
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
}

public enum Operation {
    Define,
    DefineConst,
    Swizzle,
    Add,
    Sub,
    Mul,
    Div,
}

/*

var a = 0.25f;
var b = position.y;
var c = a + b;
density = c

// density: assin to c -> c: a op_addition b, a: const float 0.25, b: position decompose y
// 

*/