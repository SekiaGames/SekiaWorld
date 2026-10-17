using Microsoft.CodeAnalysis;

namespace ET.Analyzer
{
    public static class ETTaskInSyncMethodAnalyzerRule
    {
        private const string Title = "ETTask方法调用在非异步方法体内使用错误";

        private const string MessageFormat = "方法: {0} 在非异步方法体内使用时需要添加.Coroutine()后缀";

        private const string Description = "ETTask方法调用在非异步方法体内使用错误.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ETTaskInSyncMethodAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class ETTaskInAsyncMethodAnalyzerRule
    {
        private const string Title = "ETTask方法调用在异步方法体内使用错误";

        private const string MessageFormat = "方法: {0} 在异步方法体内使用时需要添加await前缀 或 .Coroutine()后缀";

        private const string Description = "ETTask方法调用在异步方法体内使用错误.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ETTaskInAsyncMethodAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class UniqueIdRangeAnaluzerRule
    {
        private const string Title = "唯一Id字段数值区间约束";

        private const string MessageFormat = "类: {0} Id字段: {1}的值： {2} 不在约束的区间内, 请修改";

        private const string Description = "唯一Id字段数值区间约束.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.UniqueIdRangeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class UniqueIdDuplicateAnalyzerRule
    {
        private const string Title = "唯一Id字段禁止重复";

        private const string MessageFormat = "类: {0} Id字段: {1}的值： {2} 与其他Id字段值重复, 请修改";

        private const string Description = "唯一Id字段禁止重复.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.UniqueIdDuplicateAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class AddChildTypeAnalyzerRule
    {
        private const string Title = "AddChild方法类型约束错误";

        private const string MessageFormat = "Type: {0} 不允许作为实体: {1} 的AddChild函数参数类型! 若要允许该类型作为参数,请使用ChildOfAttribute对child实体类标记父级类型";

        private const string Description = "AddChild方法类型约束错误.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AddChildTypeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class DisableAccessEntityChildAnalyzerRule
    {
        private const string Title = "禁止在Entity类中直接调用Child和Component";

        private const string MessageFormat = "禁止在Entity类中直接调用Child和Component";

        private const string Description = "禁止在Entity类中直接调用Child和Component.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableUseChildComponentInEntityAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntityComponentAnalyzerRule
    {
        private const string Title = "实体类添加或获取组件类型错误";

        private const string MessageFormat = "组件类型: {0} 不允许作为实体: {1} 的组件类型! 若要允许该类型作为参数,请使用ComponentOfAttribute对组件类标记父级实体类型";

        private const string Description = "实体类添加或获取组件类型错误.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityComponentAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class StaticFieldDeclarationAnalyzerRule
    {
        private const string Title = "Static字段声明需要标记标签";

        private const string MessageFormat = "Static字段声明 {0} 需要标记标签";

        private const string Description = "Static字段声明需要标记标签.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.StaticFieldDeclarationAnalyzerRule,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class CheckETCancellTokenAfterAwaitAnalyzerRule
    {
        private const string Title = "含有ETCancelToken参数的异步函数内调用await表达式后必须判断CancelToken.IsCancel";

        private const string MessageFormat = "含有ETCancelToken参数的异步函数内调用await表达式后必须判断CancelToken.IsCancel";

        private const string Description = "含有ETCancelToken参数的异步函数内调用await表达式后必须判断CancelToken.IsCancel.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ETCancellationTokenAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class AwaitExpressionCancelTokenParamAnalyzerRule
    {
        private const string Title = "含有ETCancelToken参数的异步函数内调用await表达式必须传入同一个CancelToken";

        private const string MessageFormat = "含有ETCancelToken参数的异步函数内调用await表达式必须传入同一个CancelToken";

        private const string Description = "含有ETCancelToken参数的异步函数内调用await表达式必须传入同一个CancelToken.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AwaitExpressionCancelTokenParamAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class AsyncMethodWithCancelTokenParamAnalyzerRule
    {
        private const string Title = "异步函数声明处的ETCancelToken参数禁止声明默认值";

        private const string MessageFormat = "异步函数声明处的ETCancelToken参数禁止声明默认值";

        private const string Description = "异步函数声明处的ETCancelToken参数禁止声明默认值.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AsyncMethodWithCancelTokenParamAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class ExpressionWithCancelTokenParamAnalyzerRule
    {
        private const string Title = "函数调用处的ETCancelToken参数禁止传入null";

        private const string MessageFormat = "函数调用处的ETCancelToken参数禁止传入null";

        private const string Description = "函数调用处的ETCancelToken参数禁止传入null.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ExpressionWithCancelTokenParamAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntityClassDeclarationAnalyzerRule
    {
        private const string Title = "实体类限制多层继承";

        private const string MessageFormat = "类: {0} 不能继承{1}的子类 请直接继承{1}";

        private const string Description = "实体类限制多层继承.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.EntityClassDeclarationAnalyzerRuleId,
            Title,
            MessageFormat,
            DiagnosticCategories.All,
            DiagnosticSeverity.Error, true, Description);
    }

    public static class EntityDelegateDeclarationAnalyzerRule
    {
        private const string Title = "实体类禁止声明委托字段或属性";

        private const string MessageFormat = "实体类: {0} 不能在类内部声明委托字段或属性: {1}";

        private const string Description = "实体类禁止声明委托字段或属性.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DelegateAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntityFieldDeclarationInEntityAnalyzerRule
    {
        private const string Title = "实体类禁止声明实体字段";

        private const string MessageFormat = "实体类: {0} 不能在类内部声明实体或含有实体类参数的泛型类字段: {1} 请使用EntityRef代替";

        private const string Description = "实体类禁止声明实体字段.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityFieldDeclarationInEntityAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class AsyncMethodReturnTypeAnalyzerRule
    {
        private const string Title = "禁止声明返回值为void的异步方法";

        private const string MessageFormat = "禁止声明返回值为void的异步方法";

        private const string Description = "禁止声明返回值为void的异步方法.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AsyncMethodReturnTypeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class ClientClassInServerAnalyzerRule
    {
        private const string Title = "禁止在Server程序集内引用ET.Client命名空间";

        private const string MessageFormat = "禁止在Server程序集内引用ET.Client命名空间";

        private const string Description = "禁止在Server程序集内引用ET.Client命名空间.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ClientClassInServerAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntitySystemAnalyzerRule
    {
        private const string Title = "Entity类存在未生成的生命周期函数";

        private const string MessageFormat = "Entity类: {0} 存在未生成的生命周期函数";

        private const string Description = "Entity类存在未生成的生命周期函数.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntitySystemAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntitySystemMethodNeedSystemOfAttrAnalyzerRule
    {
        private const string Title = "EntitySystem标签只能添加在含有EntitySystemOf标签的静态类中";

        private const string MessageFormat = "方法:{0}的{1}标签只能添加在含有{2}标签的静态类中";

        private const string Description = "EntitySystem标签只能添加在含有EntitySystemOf标签的静态类中.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntitySystemMethodNeedSystemOfAttrAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class FiberLogAnalyzerRule
    {
        private const string Title = "实体类内部或含有实体类参数的函数内部必须使用Fiber输出日志";

        private const string MessageFormat = "实体类内部或含有实体类参数的函数内部必须使用Fiber输出日志";

        private const string Description = "实体类内部或含有实体类参数的函数内部必须使用Fiber输出日志.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.FiberLogAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntityHashCodeAnalyzerRule
    {
        private const string Title = "实体类HashCode禁止重复";

        private const string MessageFormat = "{0} 与 {1} 类名HashCode相同:{2}, 请修改类名保证实体类HashCode唯一";

        private const string Description = "实体类HashCode禁止重复.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityHashCodeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntityComponentChildAnalyzerRule
    {
        private const string Title = "实体类禁止同时标记为Component和Child";

        private const string MessageFormat = "实体类:{0} 禁止同时标记为Component和Child";

        private const string Description = "实体类禁止同时标记为Component和Child.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityComponentChildAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntityCannotDeclareGenericTypeRule
    {
        private const string Title = "实体类禁止声明为泛型实体类";

        private const string MessageFormat = "实体类:{0} 禁止声明为泛型实体类";

        private const string Description = "实体类禁止声明为泛型实体类.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityCannotDeclareGenericTypeRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class NetMessageAnalyzerRule
    {
        private const string Title = "消息类禁止声明实体字段";

        private const string MessageFormat = "消息类: {0} 禁止声明实体字段: {1}";

        private const string Description = "消息类禁止声明实体字段.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.NetMessageAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class DisableNewAnalyzerRule
    {
        private const string Title = "含有DisableNew标记的类禁止使用new构造对象";

        private const string MessageFormat = "禁止使用new构造{0}类型的对象";

        private const string Description = "含有DisableNew标记的类禁止使用new构造对象.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableNewAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class EntityClassDeclarationAnalyzerrRule
    {
        private const string Title = "Model/ModelView程序集禁止声明非实体类";

        private const string MessageFormat = "Model/ModelView程序集禁止声明非Object类:{0}, 除非加上[EnableClass]";

        private const string Description = "Model/ModelView程序集禁止声明非实体类, 除非加上[EnableClass] Attribute.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableNormalClassDeclaratonInModelAssemblyAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class AwaitEntityAnalyzerRule
    {
        private const string Title = "禁止在 await 之后访问 Entity 及其子类变量(ET10:ETAE001)";

        private const string MessageFormat = "变量 '{0}' 是 Entity 或其子类类型，不允许在 await 之后访问。请使用 EntityRef 包装传递！";

        private const string Description = "await 之后 Entity 可能已被销毁, 必须通过 EntityRef 重新获取. 确实安全的场景可加 [SkipAwaitEntityCheck].";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AwaitEntityAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class CoroutineLockUsingAnalyzerRule
    {
        private const string Title = "禁止对 CoroutineLock 使用 using 语句(ET10)";

        private const string MessageFormat = "CoroutineLock 是自管理锁，超时会自动释放. 请用 EntityRef<CoroutineLock> 接收并在需要时释放, 不要使用 using.";

        private const string Description = "CoroutineLock 超时自动释放，使用 using 或手动 Dispose 可能导致重复释放/语义混乱。";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.CoroutineLockUsingAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class CoroutineLockDisposeAnalyzerRule
    {
        private const string Title = "禁止手动调用 CoroutineLock.Dispose(ET10)";

        private const string MessageFormat = "禁止手动调用 CoroutineLock.Dispose，锁会在超时后自动释放. 如确需提前释放请释放 EntityRef<CoroutineLock>.";

        private const string Description = "CoroutineLock 超时自动释放，手动 Dispose 可能导致重复释放/语义混乱。";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.CoroutineLockDisposeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    public static class DisableGetComponentAnalyzerRule
    {
        private const string Title = "禁止直接使用GetComponent获取被禁用的组件";

        private const string MessageFormat = "组件类型: {0} 禁止直接使用GetComponent获取, 请在方法上添加[EnableGetComponent(typeof({0}))]";

        private const string Description = "被DisableGetComponent标记的组件禁止直接通过GetComponent获取.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableGetComponentAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }
}