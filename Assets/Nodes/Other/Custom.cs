public class CustomCodeNode<T> : Variable<T> {
    public delegate string Callback(TreeContext ctx);
    public TreeNode[] dependencies;
    public Callback callback;

    public override void HandleInternal(TreeContext ctx) {
        foreach (var dependency in dependencies) {
            dependency.Handle(ctx);
        }


        ctx.DefineAndBindNode(this, Utils.TypeOf<T>(), "custom_code", callback?.Invoke(ctx));
    }
}

public class CustomCode<T> {
    public CustomCodeNode<T>.Callback callback;
    public TreeNode[] dependencies;

    public CustomCode(CustomCodeNode<T>.Callback callback, params TreeNode[] dependencies) {
        this.callback = callback;
        this.dependencies = dependencies;
    }

    public Variable<T> DoStuff() {
        return new CustomCodeNode<T> {
            callback = callback,
            dependencies = dependencies
        };
    }
}