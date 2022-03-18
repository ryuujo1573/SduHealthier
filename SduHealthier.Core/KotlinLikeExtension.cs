using System;

namespace SduHealthier.Core
{
    public static class KotlinLikeExtension
    {
        public static TResult Let<T, TResult>(this T x, Func<T, TResult> f) => f(x);
    }
}