public class Variable<T> {
    public TreeNode symbol;

    public static implicit operator Variable<T>(T value) {
        return new Variable<T>() {
            symbol = new DefineNode { value = value.ToString(), Type = Utils.TypeOf<T>() },
        };
    }

    public static Variable<T> operator +(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new SimpleBinOpNode { a = a.symbol, b = b.symbol, op = "+" },
        };
    }

    public static Variable<T> operator -(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new SimpleBinOpNode { a = a.symbol, b = b.symbol, op = "-" },
        };
    }

    public static Variable<T> operator *(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new SimpleBinOpNode { a = a.symbol, b = b.symbol, op = "*" },
        };
    }
    public static Variable<T> operator /(Variable<T> a, Variable<T> b) {
        return new Variable<T> {
            symbol = new SimpleBinOpNode { a = a.symbol, b = b.symbol, op = "/" },
        };
    }

    public static implicit operator Variable<T>(Inject<T> value) {
        return new Variable<T> {
            symbol = new InjectedNode { calback = () => value.x },
        };
    }
}