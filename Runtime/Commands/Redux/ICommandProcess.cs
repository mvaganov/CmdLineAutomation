using System;

namespace RunCmdRedux {
	/// <summary>
	/// Processing logic to respond to a string command input. The main method is a non-blocking
	/// cooperative-multithreaded function. This is a base command processing interface.
	/// </summary>
	public interface ICommandProcess : IDisposable {
		public enum State { None, Disabled, Executing, Finished, Cancelled, Error }
		/// <summary>
		/// Event handling function, which starts a command. The command line system uses a cooperative
		/// threading model, with status retrieved by <see cref="ExecutionState"/>.
		/// </summary>
		/// <param name="command">The command being executed</param>
		/// <param name="print">Where the results of this command will go, one line at a time</param>
		public void StartCooperativeFunction(string command, PrintCallback print);

		/// <summary>
		/// Poll after <see cref="StartCooperativeFunction(string, PrintCallback)"/> to
		/// determine if this command is finished processing.
		/// </summary>
		/// <returns>true when the command is finished</returns>
		public State ExecutionState { get; }

		/// <summary>
		/// Estimate of progress. Return less-than-or-equal-to zero for fallback behavior
		/// </summary>
		public float GetProgress();

		/// <summary>
		/// A callback that should be implemented to be called when the process is done
		/// </summary>
		public event Action OnFinish;

		/// <summary>
		/// Success response
		/// </summary>
		public object Result { get; }

		/// <summary>
		/// Error response
		/// </summary>
		public object Error { get; }

		/// <summary>
		/// Optionally implemented method to service the cooperative function
		/// </summary>
		public void ContinueCooperativeFunction();
	}

	public interface INamedProcess : ICommandProcess {
		public string name { get; }
	}

	public abstract class BaseProcess : ICommandProcess {
		public virtual event Action OnFinish = delegate { };
		public abstract ICommandProcess.State ExecutionState { get; }
		public abstract float GetProgress();
		public abstract void StartCooperativeFunction(string command, PrintCallback print);
		public virtual object Result => null;
		public virtual object Error => null;
		public virtual void ContinueCooperativeFunction() { }
		public void Dispose() { }
	}

	public abstract class BaseNamedProcess : BaseProcess, INamedProcess {
		protected ICommandProcess.State _state = ICommandProcess.State.None;
		public abstract string name { get; }
		public override ICommandProcess.State ExecutionState => _state;
	}
}
