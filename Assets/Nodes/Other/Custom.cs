public class CustomCodeNode<T> : Variable<T> {
    public delegate string Callback(TreeContext ctx);

    public Callback callback;

    public override void HandleInternal(TreeContext ctx) {
        ctx.DefineAndBindNode(this, Utils.TypeOf<T>(), "custom_code", callback?.Invoke(ctx));
    }
}

public class CustomCode<T> {
    public CustomCodeNode<T>.Callback callback;

    public CustomCode(CustomCodeNode<T>.Callback callback) {
        this.callback = callback;
    }

    public Variable<T> DoStuff() {
        return new CustomCodeNode<T> {
            callback = callback,
        };
    }
}