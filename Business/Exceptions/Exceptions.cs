using System;
using System.Diagnostics;


namespace TodoList.Business.Exceptions
{
    // From https://gitlab.com/Framework.Net/ApplicationBase project TechnicalTools

    /// <summary>
    /// Base type of all exception of this file.
    /// This allows to distinguish between expected exception and unexpected exception.
    /// This also indicate if exception is from internal product / company or elsewhere.
    /// This knowledge can used to reassure user because we know the knowledge of why
    /// the exception occured is readable by support/developper.
    /// </summary>
	[Serializable]
    public class BaseException : Exception, IBaseException
    {
        /// <summary>
        /// Get the Id of this Bug linked to this exception and all others
        /// This Id is display to user so MOE can follow the error in log
        /// or wherever there is a trace.
        /// </summary>
        public string BugId { get; private set; }

        readonly StackFrame _enrichingFrame;

        // l'argument innerException n'a pas de valeur null par defaut afin que les developpeurs n'oublient jamais cette precieuse information !
        public BaseException(string message, Exception innerException)
            : this(message, innerException, null)
        { }

        // l'argument innerException n'a pas de valeur null par defaut afin que les developpeurs n'oublient jamais cette precieuse information !
        public BaseException(string message, Exception innerException, StackFrame enrichingFrame)
            : base(message, innerException)
        {
            _enrichingFrame = enrichingFrame;

            // Si un id d'exception a déja été généré, on assure le suivi
            if (innerException is BaseException bex)
                BugId = bex.BugId;
            else
            { // sinon on le crée de façon simple
                DateTime now = DateTime.Now;
                BugId = now.ToString("yyyyMMddhhmmss");
            }

            if (innerException != null)
                FixMicrosoftBug(innerException);
        }

        #region Workaround to fix a microsoft annoyance / bug

        // https://stackoverflow.com/questions/347502/why-does-the-inner-exception-reach-the-threadexception-handler-and-not-the-actua
        // https://connect.microsoft.com/VisualStudio/feedback/details/386582/control-invoke-exception-handling
        // https://connect.microsoft.com/VisualStudio/feedback/details/433765/control-invoke-throws-exception-getbaseexception-rather-than-the-actual-exception
        private void FixMicrosoftBug(Exception innerException)
        {
            Debug.Assert(!innerException.Data.Contains("WrappingException"));
            innerException.Data["WrappingException"] = this;
        }
        public static Exception TryFindWrappingException(Exception ex)
        {
            while (ex.Data.Contains("WrappingException"))
                ex = (Exception)ex.Data["WrappingException"];
            return ex;
        }

        #endregion

        public virtual string DisplayAsSimpleMessageBoxTitle
        {
            get { return null; }
        }
    }
    public interface IBaseException { }

    /// <summary>
    /// Base class for all issue the default user can understand.
    /// The message must be clear and any tips should be provided to make user to fix himself the problem (without the need of a developper)
    /// For example :
    /// <para>
    /// "\"45aaa\" is not a number !"
    /// => User is able t ofix the problem himself without the help of anyone.
    /// </para>
    /// <para>
    /// "You are not allowed to perform this action!"
    /// => user can ask to his manager to get the good right.
    /// The manager is the only one that can check the user should really expect to have this right
    /// If this is the case manager will open a ticket as a bug.
    /// Developpers are not supposed to know the right of all users.
    /// </para>
    /// <para>
    /// "Cannot connect to network drive to get file "foo"! Please ask IT department about this issue."
    /// (ie: We are not talking about developper here, so from a developper perspective, user is autonomous to fix the problem)
    /// </para>
    /// See also <seealso cref="BusinessException">BusinessException</see> that inherits from UserUnderstandableException.
    /// See also <seealso cref="CriticalException"> or child classes</see> for serious error related to bad data.
    /// </summary>
    [Serializable]
    public class UserUnderstandableException : BaseException, IUserUnderstandableException
    {
        public UserUnderstandableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    public interface IUserUnderstandableException : IBaseException { }

    /// <summary>
    /// This class is for any error related to company business.
    /// These kind of error are the most common ones and can occur frequently (just because user forget business rules sometimes...)
    /// Example (let's say our company is developping an application to manage user's family tree):
    /// "You cannot make this person married to X because it is marked as already married to Y on the same period."
    /// </summary>
    [Serializable]
    public class BusinessException : UserUnderstandableException, IBusinessException
    {
        public BusinessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    public interface IBusinessException : IUserUnderstandableException { }

    [Serializable]
    public class OutdatedDataException : UserUnderstandableException, IOutdatedDataException
    {
        public OutdatedDataException()
            : base("The data has been changed by someone else or something else. Please refresh and retry!", null)
        {
        }
    }
    public interface IOutdatedDataException : IUserUnderstandableException { }

    /// <summary>
    /// Hack of the exception flow in order to cancel some operation without any actual error.
    /// This class inherits of BusinessException which is a minor error.
    /// </summary>
    /// Example : User want to cancel an operation that is executed in a thread using business logic and data...
    ///           We can check and inject a SilentBusinessException.
    ///           Killing a thread (that cause ThreadAbortException) is a _really_n _really_, bad idea for a lot of good reasons
    ///           (see Eric Lippert's article if you want to know more : dead lock can happens, etc).
    public class SilentBusinessException : BusinessException, ISilentBusinessException
    {
        public SilentBusinessException(string msg)
            : base(msg, null)
        {
        }
        public SilentBusinessException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }
    }
    public interface ISilentBusinessException : IBusinessException { }

    /// <summary>
    /// Technical error identified by a developper, which can be fixed by him (example : a configuration is bad).
    /// This kind of error should never happens of course (in a perfect world...)
    /// This class allow to distinguish between unexpected exception (Exception class or any other type) from the case identified by developpers themselves.
    /// Thus, generic exception treatment can take measures about this fact.
    /// Please note also that TechnicalException does not necessary means "urgent"
    /// (for exception that a developper must fix urgently, see <see cref="CriticalException">CriticalException</see>)
    /// </summary>
    [Serializable]
    public class TechnicalException : BaseException, ITechnicalException
    {
        public TechnicalException(string message, Exception innerException)
            : this(message, innerException, null)
        { }

        public TechnicalException(string message, Exception innerException, StackFrame enrichingFrame)
            : base(message, innerException, enrichingFrame)
        {
        }
    }
    public interface ITechnicalException : IBaseException { }

    /// <summary>
    /// Critical exception are when an IT developper is needed urgently to fix the problem
    /// This class must be used, preferably to <see cref="TechnicalIntegrityException"/>,
    /// when all developpers of the current product do not know or are not able to fix the issue
    /// because they are depending on any other team or any external company.
    /// </summary>
    [Serializable]
    public class CriticalException : TechnicalException, ICriticalException
    {
        public CriticalException(string message, Exception innerException)
            : this(message, innerException, null)
        { }

        public CriticalException(string message, Exception innerException, StackFrame enrichingFrame)
            : base(message, innerException, enrichingFrame)
        {
        }
    }
    public interface ICriticalException : ITechnicalException { }


    /// <summary>
    /// Serious error when data are corrupted or inconsistent and a developper is needed to fix issue.
    /// In an ideal world this should not happens because workflow should prevent this to be possible.
    /// And yet...
    /// For example if an action on multiple databases fails in the middle of the work and because distributed transaction is not always safe
    /// this is the exception class to use.
    /// This class must be used, preferably to <see cref="CriticalException"/>, when any developper of the current product _knows_ how
    /// and is able to fix the issue without depending on any other team or any external company.
    /// </summary>
    [Serializable]
    public class TechnicalIntegrityException : CriticalException, ITechnicalIntegrityException
    {
        public Exception OriginalError { get; private set; } // Can be null !
        public Exception RollbackError { get; private set; } // Can be null !

        /// <param name="message">Any business or technical message indicating what's broken</param>
        /// <param name="originalError">Original unexpected error that make everything... complicated in the first place.
        ///                             For example the exception that occured in second part of a _distributed_ transaction, (think about no network)</param>
        /// <param name="rollbackError">Exception that occured when trying to fix thing or null if no fix has been tried.
        ///                             Think about : no network to undo the first step of distributed transaction</param>
        /// <param name="actionToDo">A custom advice for user</param>
        public TechnicalIntegrityException(string message, Exception rollbackError, Exception originalError, string actionToDo = "Call an IT guy!")
            : base(message, rollbackError ?? originalError) // rollbackError should contains originalError as InnerException (or at any level)
        {
            RollbackError = rollbackError;
            OriginalError = originalError;
        }
    }
    public interface ITechnicalIntegrityException : ICriticalException { }


    #region Technical exceptions for check wich can be kintercept in a Gui to be customized (sexier message for end user)

    public class TechnicalDbConstraintMinimalValue : TechnicalException
    {
        public IConvertible MinimalValue { get; set; }
        public IConvertible CurrentValue { get; set; }

        public TechnicalDbConstraintMinimalValue(string message, IConvertible currentValue, IConvertible minimalValue)
            : base(message, null)
        {
            MinimalValue = minimalValue;
            CurrentValue = currentValue;
        }
    }
    public class TechnicalDbConstraintMaximalValue : TechnicalException, ITechnicalDbConstraint
    {
        public IConvertible MaximalValue { get; set; }
        public IConvertible CurrentValue { get; set; }

        public TechnicalDbConstraintMaximalValue(string message, IConvertible currentValue, IConvertible maximalValue)
            : base(message, null)
        {
            MaximalValue = maximalValue;
            CurrentValue = currentValue;
        }
    }
    public class TechnicalDbConstraintMinimumLength : TechnicalException, ITechnicalDbConstraint
    {
        public long MinimumLength { get; set; }
        public long CurrentLength { get; set; }

        public TechnicalDbConstraintMinimumLength(string message, long currentLength, long minimumLength)
            : base(message, null)
        {
            MinimumLength = minimumLength;
            CurrentLength = currentLength;
        }
    }
    public class TechnicalDbConstraintMaximumLength : TechnicalException, ITechnicalDbConstraint
    {
        public long MaximalLength { get; set; }
        public long CurrentLength { get; set; }

        public TechnicalDbConstraintMaximumLength(string message, long currentLength, long maximalLength)
            : base(message, null)
        {
            MaximalLength = maximalLength;
            CurrentLength = currentLength;
        }
    }
    public class TechnicalDbConstraintNotNullable : TechnicalException, ITechnicalDbConstraint
    {
        public TechnicalDbConstraintNotNullable(string message)
            : base(message, null)
        {
        }
    }
    public class TechnicalDbConstraintDateTimeHaveToMuchPrecision : TechnicalException, ITechnicalDbConstraint
    {
        public DateTime DateValue { get; private set; }
        public bool TimePartIsAllowed { get; private set; }
        public byte TimePartPrecision { get; private set; }
        public TechnicalDbConstraintDateTimeHaveToMuchPrecision(string message, DateTime value, bool timePartIsAllowed, byte timePartPrecision)
            : base(message, null)
        {
            DateValue = value;
            TimePartIsAllowed = timePartIsAllowed;
            TimePartPrecision = timePartPrecision;
        }
    }
    public interface ITechnicalDbConstraint : ITechnicalException { }
    #endregion
}
