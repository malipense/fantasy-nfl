using System.Globalization;

namespace Game.Infrastructure
{
    public static class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = MakeTask<object>(null);
        public static Task Empty
        {
            get
            {
                return _emptyTask;
            }
        }
        private static Task<T> MakeTask<T>(T value)
        {
            return FromResult<T>(value);
        }
        public static Task Then<T1, T2, T3>(this Task task, Func<T1, T2, T3, Task> sucessor, T1 arg1, T2 arg2, T3 arg3)
        {
            switch (task.Status) 
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(sucessor, arg1, arg2, arg3);

                default:
                    return GenericDelegates<object, Task, T1, T2, T3>.ThenWithArgs(task, sucessor, arg1, arg2, arg3)
                        .FastUnwrap();
            }

        }

        public static Task FromMethod<T1, T2, T3>(Func<T1, T2, T3, Task> func, T1 arg1, T2 arg2, T3 arg3)
        {
            try
            {
                return func(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task FromMethod<T1>(Action<T1> func, T1 arg)
        {
            try
            {
                func(arg);
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.TrySetResult(value);
            return tcs.Task;
        }

        internal static Task FromError(Exception e)
        {
            return FromError<object>(e);
        }
        internal static Task<T> FromError<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.TrySetUnwrappedException<T>(e);
            return tcs.Task;
        }

        internal static bool TrySetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e)
        {
            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                return tcs.TrySetException(aggregateException.InnerExceptions);
            }
            else
            {
                return tcs.TrySetException(e);
            }
        }

        public static Task Finally(this Task task, Action<object> next, object state)
        {
            try
            {
                switch (task.Status)
                {
                    case TaskStatus.Faulted:
                    case TaskStatus.Canceled:
                        next(state);
                        return task;
                    case TaskStatus.RanToCompletion:
                        return FromMethod(next, state);

                    default:
                        return RunTaskSynchronously(task, next, state, onlyOnSuccess: false);
                }
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        private static Task RunTaskSynchronously(Task task, Action<object> next, object state, bool onlyOnSuccess = true)
        {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWithPreservedCulture(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        if (!onlyOnSuccess)
                        {
                            next(state);
                        }

                        tcs.TrySetUnwrappedException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        if (!onlyOnSuccess)
                        {
                            next(state);
                        }

                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        next(state);
                        tcs.TrySetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetUnwrappedException(ex);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        private static class GenericDelegates<T, TResult, T1, T2, T3>
        {
            internal static Task<Task> ThenWithArgs(Task task, Func<T1, T2, T3, Task> sucessor, T1 arg1, T2 arg2, T3 arg3)
            {
                return TaskRunners<object, Task>.RunTask(task, () => sucessor(arg1, arg2, arg3));
            }
        }

        private static class TaskRunners<T, TResult>
        {
            internal static Task<TResult> RunTask(Task task, Func<TResult> successor)
            {
                var tcs = new TaskCompletionSource<TResult>();
                task.ContinueWithPreservedCulture(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.TrySetUnwrappedException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        try
                        {
                            tcs.TrySetResult(successor());
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }

            internal static Task RunTask(Task<T> task, Action<T> successor)
            {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWithPreservedCulture(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.TrySetUnwrappedException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        try
                        {
                            successor(t.Result);
                            tcs.TrySetResult(null);
                        }
                        catch(Exception e)
                        {
                            tcs.TrySetUnwrappedException(e);
                        }
                    }
                });
                return tcs.Task;
            }
        }
        internal static Task ContinueWithPreservedCulture(this Task task, Action<Task> continuationAction)
        {
            return task.ContinueWithPreservedCulture(continuationAction, TaskContinuationOptions.None);
        }
        internal static Task ContinueWithPreservedCulture<T>(this Task<T> task, Action<Task<T>> continuationAction)
        {
            return task.ContinueWithPreservedCulture(continuationAction, TaskContinuationOptions.None);
        }

        internal static Task<TResult> ContinueWithPreservedCulture<T, TResult>(this Task<T> task, Func<Task<T>, TResult> continuationAction)
        {
            return task.ContinueWithPreservedCulture(continuationAction, TaskContinuationOptions.None);
        }
        internal static Task ContinueWithPreservedCulture(this Task task, Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
        {
#if NETSTANDARD1_3
            // The Thread class is not available on .NET Standard 1.3
            return task.ContinueWith(continuationAction, continuationOptions);
#else
            var preservedCulture = SaveCulture();
            return task.ContinueWith(t => RunWithPreservedCulture(preservedCulture, continuationAction, t), continuationOptions);
#endif
        }

        internal static Task ContinueWithPreservedCulture<T>(this Task<T> task, Action<Task<T>> continuationAction, TaskContinuationOptions continuationOptions)
        {
#if NETSTANDARD1_3
            // The Thread class is not available on .NET Standard 1.3
            return task.ContinueWith(continuationAction, continuationOptions);
#else
            var preservedCulture = SaveCulture();
            return task.ContinueWith(t => RunWithPreservedCulture(preservedCulture, continuationAction, t), continuationOptions);
#endif
        }

        internal static Task<TResult> ContinueWithPreservedCulture<T, TResult>(this Task<T> task, Func<Task<T>, TResult> continuationAction, TaskContinuationOptions continuationOptions)
        {
#if NETSTANDARD1_3
            // The Thread class is not available on .NET Standard 1.3
            return task.ContinueWith(continuationAction, continuationOptions);
#else
            var preservedCulture = SaveCulture();
            return task.ContinueWith(t => RunWithPreservedCulture(preservedCulture, continuationAction, t), continuationOptions);
#endif
        }

        internal static TResult RunWithPreservedCulture<T1, T2, TResult>(CulturePair preservedCulture, Func<T1, T2, TResult> func, T1 arg1, T2 arg2)
        {
            var replacedCulture = SaveCulture();
            try
            {
                Thread.CurrentThread.CurrentCulture = preservedCulture.Culture;
                Thread.CurrentThread.CurrentUICulture = preservedCulture.UICulture;
                return func(arg1, arg2);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = replacedCulture.Culture;
                Thread.CurrentThread.CurrentUICulture = replacedCulture.UICulture;
            }
        }

        internal static TResult RunWithPreservedCulture<T, TResult>(CulturePair preservedCulture, Func<T, TResult> func, T arg)
        {
            return RunWithPreservedCulture(preservedCulture, (f, state) => f(state), func, arg);
        }

        internal static void RunWithPreservedCulture<T>(CulturePair preservedCulture, Action<T> action, T arg)
        {
            RunWithPreservedCulture(preservedCulture, (f, state) =>
            {
                f(state);
                return (object)null;
            },
            action, arg);
        }

        internal static void RunWithPreservedCulture(CulturePair preservedCulture, Action action)
        {
            RunWithPreservedCulture(preservedCulture, f => f(), action);
        }

#if !NETSTANDARD1_3
        internal struct CulturePair
        {
            public CultureInfo Culture;
            public CultureInfo UICulture;
        }

        internal static CulturePair SaveCulture()
        {
            return new CulturePair
            {
                Culture = Thread.CurrentThread.CurrentCulture,
                UICulture = Thread.CurrentThread.CurrentUICulture
            };
        }

        public static Task FastUnwrap(this Task<Task> task)
        {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        public static Task<T> FastUnwrap<T>(this Task<Task<T>> task)
        {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }
    }
#endif
}