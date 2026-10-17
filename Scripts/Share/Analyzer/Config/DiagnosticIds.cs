namespace ET.Analyzer
{
    public static class DiagnosticIds
    {
        public const string AddChildTypeAnalyzerRuleId = "ET0001";

        public const string EntityFiledAccessAnalyzerRuleId = "ET0002";

        public const string EntityClassDeclarationAnalyzerRuleId = "ET0003";

        public const string HotfixProjectFieldDeclarationAnalyzerRuleId = "ET0004";

        public const string ClassDeclarationInHotfixAnalyzerRuleId = "ET0005";
        
        public const string EntityMethodDeclarationAnalyzerRuleId = "ET0006";

        public const string EntityComponentAnalyzerRuleId = "ET0007";
        
        public const string ETTaskInSyncMethodAnalyzerRuleId = "ET0008";
        
        public const string ETTaskInAsyncMethodAnalyzerRuleId = "ET0009";

        public const string DelegateAnalyzerRuleId = "ET0010";

        public const string UniqueIdRangeAnalyzerRuleId = "ET0011";
        
        public const string UniqueIdDuplicateAnalyzerRuleId = "ET0012";

        public const string StaticClassCircularDedendencyAnalyzerRuleId = "ET0013";

        public const string DisableUseChildComponentInEntityAnalyzerRuleId = "ET0014";

        public const string StaticFieldDeclarationAnalyzerRule = "ET0015";

        public const string ETCancellationTokenAnalyzerRuleId = "ET0016";

        public const string AwaitExpressionCancelTokenParamAnalyzerRuleId = "ET0017";

        public const string AsyncMethodWithCancelTokenParamAnalyzerRuleId = "ET0018";

        public const string ExpressionWithCancelTokenParamAnalyzerRuleId = "ET0019";

        public const string EntityFieldDeclarationInEntityAnalyzerRuleId = "ET0020";

        public const string AsyncMethodReturnTypeAnalyzerRuleId = "ET0021";

        public const string ClientClassInServerAnalyzerRuleId = "ET0022";

        // ET0023 已退休（原 LSEntity 禁浮点字段，随 LockStep 删除）—— 编号不复用。
        public const string EntitySystemAnalyzerRuleId = "ET0024";

        public const string EntitySystemMethodNeedSystemOfAttrAnalyzerRuleId = "ET0025";

        public const string FiberLogAnalyzerRuleId = "ET0026";
        
        
        public const string EntityHashCodeAnalyzerRuleId = "ET0027";

        public const string EntityComponentChildAnalyzerRuleId = "ET0028";

        public const string EntityCannotDeclareGenericTypeRuleId = "ET0029";

        public const string NetMessageAnalyzerRuleId = "ET0030";

        public const string DisableNewAnalyzerRuleId = "ET0031";

        public const string DisableNormalClassDeclaratonInModelAssemblyAnalyzerRuleId = "ET0032";

        public const string AwaitEntityAnalyzerRuleId = "ET0033";

        public const string CoroutineLockUsingAnalyzerRuleId = "ET0034";

        public const string CoroutineLockDisposeAnalyzerRuleId = "ET0035";

        // ET0036 上游是 TestCaseNaming（测试框架，本仓未搬）—— 编号不复用，直接跳到上游的 ET0037
        public const string DisableGetComponentAnalyzerRuleId = "ET0037";
    }
}