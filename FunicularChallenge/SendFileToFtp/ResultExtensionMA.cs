using FunicularSwitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SendFileToFtp
{
    public static partial class ResultExtensionMA
    {
        /// <summary>
        /// The local extension of the synchronous static Result.Try(), passing the current instance as parameter.
        /// </summary>
        /// <typeparam name="T">Any type compatible with Result.</typeparam>
        /// <param name="item">Typed Result instance to pass as parameter to the action.</param>
        /// <param name="action">The action to execute on the result.</param>
        /// <param name="formatError">Formatting function in case an exception is caught.</param>
        /// <returns>The original Return instance, unless an exception occurred.</returns>
        /// <exception cref="InvalidOperationException">Should never happen in practice.</exception>
        public static Result<T> Try<T>(this Result<T> item, Action<T> action, Func<Exception, string> formatError)
        {
            switch(item)
            {
                case FunicularSwitch.Result<T>.Ok_ ok:
                    try
                    {
                        action(ok.Value);
                        return item;
                    }
                    catch(Exception e)
                    {
                        return Result.Error<T>(formatError(e));
                    }
                case FunicularSwitch.Result<T>.Error_:
                    return item;
                default:
                    throw new InvalidOperationException($"Unexpected derived result type: {item.GetType()}");
            }
        }

        /// <summary>
        /// The local extension of the asynchronous static Result.Try(), passing the current instance as parameter.
        /// </summary>
        /// <typeparam name="T">Any type compatible with Result.</typeparam>
        /// <param name="item">Typed Result instance to pass as parameter to the action.</param>
        /// <param name="action">The action to execute on the result.</param>
        /// <param name="formatError">Formatting function in case an exception is caught.</param>
        /// <returns>The original Return instance, unless an exception occurred.</returns>
        /// <exception cref="InvalidOperationException">Should never happen in practice.</exception>
        public static async Task<Result<T>> Try<T>(this Result<T> item, Func<T, Task> action, Func<Exception, string> formatError)
        {
            switch(item)
            {
                case FunicularSwitch.Result<T>.Ok_ ok:
                    try
                    {
                        await action(ok.Value);
                        return item;
                    }
                    catch(Exception e)
                    {
                        return Result.Error<T>(formatError(e));
                    }
                case FunicularSwitch.Result<T>.Error_:
                    return item;
                default:
                    throw new InvalidOperationException($"Unexpected derived result type: {item.GetType()}");
            }
        }
        /// <summary>
        /// A Bind() variant allowing for and handling exceptions that occur.
        /// </summary>
        /// <typeparam name="T1">In-type, any compatible with Result.</typeparam>
        /// <typeparam name="T2">Out-type, any compatible with Result.</typeparam>
        /// <param name="item">Typed Result instance to pass as parameter to the action.</param>
        /// <param name="action">The action to execute on the result.</param>
        /// <param name="formatError">Formatting function in case an exception is caught.</param>
        /// <returns>The output of the action, unless an exception occurred.</returns>
        /// <exception cref="InvalidOperationException">Should never happen in practice.</exception>
        public static Result<T2> TryBind<T1, T2>(this Result<T1> item, Func<T1, Result<T2>> action, Func<Exception, string> formatError)
        {
            switch(item)
            {
                case FunicularSwitch.Result<T1>.Ok_ ok:
                    try
                    {
                        return action(ok.Value);
                    }
                    catch(Exception e)
                    {
                        return Result.Error<T2>(formatError(e));
                    }
                case FunicularSwitch.Result<T1>.Error_ error:
                    return error.Convert<T2>();
                default:
                    throw new InvalidOperationException($"Unexpected derived result type: {item.GetType()}");
            }
        }

        /// <summary>
        /// An asynchronous Bind() variant allowing for and handling exceptions that occur.
        /// </summary>
        /// <typeparam name="T1">In-type, any compatible with Result.</typeparam>
        /// <typeparam name="T2">Out-type, any compatible with Result.</typeparam>
        /// <param name="item">Typed Result instance to pass as parameter to the action.</param>
        /// <param name="action">The action to execute on the result.</param>
        /// <param name="formatError">Formatting function in case an exception is caught.</param>
        /// <returns>The output of the action, unless an exception occurred.</returns>
        /// <exception cref="InvalidOperationException">Should never happen in practice.</exception>
        public static async Task<Result<T2>> TryBind<T1, T2>(this Result<T1> item, Func<T1, Task<Result<T2>>> action, Func<Exception, string> formatError)
        {
            switch(item)
            {
                case FunicularSwitch.Result<T1>.Ok_ ok:
                    try
                    {
                        return await action(ok.Value);
                    }
                    catch(Exception e)
                    {
                        return Result.Error<T2>(formatError(e));
                    }
                case FunicularSwitch.Result<T1>.Error_ error:
                    return error.Convert<T2>();
                default:
                    throw new InvalidOperationException($"Unexpected derived result type: {item.GetType()}");
            }
        }

        /// <summary>
        /// Extension of the Result.Validate() method, passing an additional descriptor parameter to be used in validate().
        /// </summary>
        /// <typeparam name="T">Any type compatible with Result.</typeparam>
        /// <param name="item">Result instance to validate.</param>
        /// <param name="validate">Validation function taking an instance of T and a string descriptor as parameters.</param>
        /// <param name="descriptor">String descriptor passed to the validate function.</param>
        /// <returns>Original Result instance if no exception is thrown, error otherwise.</returns>
        public static Result<T> Validate<T>(this Result<T> item, Func<T, string, IEnumerable<string>> validate, string descriptor) => item.Bind(i => i.Validate(validate, descriptor));

        /// <summary>
        /// Extension of the Result.Validate() method, passing an additional descriptor parameter to be used in validate().
        /// </summary>
        /// <typeparam name="T">Any type compatible with Result.</typeparam>
        /// <param name="item">Any instance of T to validate.</param>
        /// <param name="validate">Validation function taking an instance of T and a string descriptor as parameters.</param>
        /// <param name="descriptor">String descriptor passed to the validate function.</param>
        /// <returns>Original Result instance if no exception is thrown, error otherwise.</returns>
        public static Result<T> Validate<T>(this T item, Func<T, string, IEnumerable<string>> validate, string descriptor)
        {
            try
            {
                var errors = validate(item, descriptor).ToList();
                return errors.Count > 0
                    ? Result.Error<T>(string.Join(ResultExtension.ErrorSeparator, errors))
                    : item;
            }
            catch(Exception)
            {
                throw; //createGenericErrorResult
            }
        }
    }
}
