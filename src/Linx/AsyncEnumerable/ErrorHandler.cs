﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Provides common error handling.
    /// </summary>
    [DebuggerNonUserCode]
    internal struct ErrorHandler
    {
        /// <summary>
        /// <see cref="ObjectDisposedException"/> singleton that says a <see cref="System.Collections.Generic.IAsyncEnumerator{T}"/> was disposed.
        /// </summary>
        public static ObjectDisposedException EnumeratorDisposedException { get; } = new ObjectDisposedException("IAsyncEnumerator");

        /// <summary>
        /// Use this rather than the default constructor.
        /// </summary>
        public static ErrorHandler Init() => new ErrorHandler(default);

        /// <summary>
        /// A <see cref="CancellationTokenRegistration"/> that is disposed along with the internal token requesting cancellation.
        /// </summary>
        public CancellationTokenRegistration ExternalRegistration;

        private readonly CancellationTokenSource _ctsInternal;
        private Exception _internalError, _externalError;

        // ReSharper disable once UnusedParameter.Local
        private ErrorHandler(Unit _)
        {
            _ctsInternal = new CancellationTokenSource();
            _internalError = _externalError = null;
            ExternalRegistration = default;
        }

        /// <summary>
        /// Gets the internal token.
        /// </summary>
        public CancellationToken InternalToken => _ctsInternal.Token;

        /// <summary>
        /// Gets the effective error condition of the enumeration.
        /// </summary>
        /// <value>The internal error if it's not an OCE on the internal token, the external error otherwise.</value>
        public Exception Error => _internalError is OperationCanceledException oce && oce.CancellationToken == _ctsInternal.Token ? _externalError : _internalError;

        /// <summary>
        /// Set the external error.
        /// </summary>
        public void SetExternalError(Exception error) => _externalError = error;

        /// <summary>
        /// Set the internal error.
        /// </summary>
        /// <remarks>Supercedes a previous error if more severe (Exception > OCE(InternalToken) > null).</remarks>
        public void SetInternalError(Exception error)
        {
            if (error != null && (_internalError == null || _internalError is OperationCanceledException oce && oce.CancellationToken == _ctsInternal.Token))
                _internalError = error;
        }

        /// <summary>
        /// Disposes the <see cref="ExternalRegistration"/> and requests cancellation on the <see cref="InternalToken"/>.
        /// </summary>
        public void Cancel()
        {
            ExternalRegistration.Dispose(); // no exception assumed
            try { _ctsInternal.Cancel(); } catch { /**/ }
        }

        /// <summary>
        /// Throws the <see cref="Error"/> if it's not null.
        /// </summary>
        public void ThrowIfError()
        {
            var error = Error;
            if (error != null) throw error;
        }
    }
}
