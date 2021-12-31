using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PianoBot_VirtualPiano_GMod.Imported {
	/// <summary>
	/// A class that manages a global low level keyboard hook
	/// </summary>
	internal class GlobalKeyboardHook {
		#region Constant, Structure and Delegate Definitions
		/// <summary>
		/// defines the callback type for the hook
		/// </summary>
		private delegate int KeyboardHookProc(int code, int wParam, ref KeyboardHookStruct lParam);

		public struct KeyboardHookStruct {
			public int VkCode;
			public int ScanCode;
			public int Flags;
			public int Time;
			public int DwExtraInfo;
		}

		const int WhKeyboardLl = 13;
		const int WmKeydown = 0x100;
		const int WmKeyup = 0x101;
		const int WmSyskeydown = 0x104;
		const int WmSyskeyup = 0x105;
		#endregion

		#region Instance Variables
		/// <summary>
		/// The collections of keys to watch for
		/// </summary>
		public List<Keys> HookedKeys = new List<Keys>();
		/// <summary>
		/// Handle to the hook, need this to unhook and call the next hook
		/// </summary>
		IntPtr _hhook = IntPtr.Zero;

        /// <summary>
        /// Added as a fix for the hookProc getting eaten by the garbage collector
        /// </summary>
        KeyboardHookProc _hookProcDelegate;
		#endregion

		#region Events
		/// <summary>
		/// Occurs when one of the hooked keys is pressed
		/// </summary>
		public event KeyEventHandler? KeyDown;
		/// <summary>
		/// Occurs when one of the hooked keys is released
		/// </summary>
		public event KeyEventHandler? KeyUp;
		#endregion

		#region Constructors and Destructors
		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalKeyboardHook"/> class and installs the keyboard hook.
		/// </summary>
		public GlobalKeyboardHook() {
            _hookProcDelegate = HookProc;
            Hook();
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="GlobalKeyboardHook"/> is reclaimed by garbage collection and uninstalls the keyboard hook.
		/// </summary>
		~GlobalKeyboardHook() {
			Unhook();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Installs the global hook
		/// </summary>
		public void Hook() {
			IntPtr hInstance = LoadLibrary("User32");
			_hhook = SetWindowsHookEx(WhKeyboardLl, _hookProcDelegate, hInstance, 0);
		}

		/// <summary>
		/// Uninstalls the global hook
		/// </summary>
		public void Unhook() {
			UnhookWindowsHookEx(_hhook);
		}

		/// <summary>
		/// The callback for the keyboard hook
		/// </summary>
		/// <param name="code">The hook code, if it isn't >= 0, the function shouldn't do anyting</param>
		/// <param name="wParam">The event type</param>
		/// <param name="lParam">The keyhook event information</param>
		/// <returns></returns>
		public int HookProc(int code, int wParam, ref KeyboardHookStruct lParam) {
			if (code < 0) return CallNextHookEx(_hhook, code, wParam, ref lParam);
			Keys key = (Keys)lParam.VkCode;
			if (!HookedKeys.Contains(key)) return CallNextHookEx(_hhook, code, wParam, ref lParam);
			KeyEventArgs kea = new KeyEventArgs(key);
			switch (wParam)
			{
				case WmKeydown or WmSyskeydown when (KeyDown != null):
					KeyDown(this, kea) ;
					break;
				case WmKeyup or WmSyskeyup when (KeyUp != null):
					KeyUp(this, kea);
					break;
			}
			return kea.Handled ? 1 : CallNextHookEx(_hhook, code, wParam, ref lParam);
		}
		#endregion

		#region DLL imports
		/// <summary>
		/// Sets the windows hook, do the desired event, one of hInstance or threadId must be non-null
		/// </summary>
		/// <param name="idHook">The id of the event you want to hook</param>
		/// <param name="callback">The callback.</param>
		/// <param name="hInstance">The handle you want to attach the event to, can be null</param>
		/// <param name="threadId">The thread you want to attach the event to, can be null</param>
		/// <returns>a handle to the desired hook</returns>
		[DllImport("user32.dll")]
		static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookProc callback, IntPtr hInstance, uint threadId);

		/// <summary>
		/// Unhooks the windows hook.
		/// </summary>
		/// <param name="hInstance">The hook handle that was returned from SetWindowsHookEx</param>
		/// <returns>True if successful, false otherwise</returns>
		[DllImport("user32.dll")]
		static extern bool UnhookWindowsHookEx(IntPtr hInstance);

		/// <summary>
		/// Calls the next hook.
		/// </summary>
		/// <param name="idHook">The hook id</param>
		/// <param name="nCode">The hook code</param>
		/// <param name="wParam">The wparam.</param>
		/// <param name="lParam">The lparam.</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KeyboardHookStruct lParam);

		/// <summary>
		/// Loads the library.
		/// </summary>
		/// <param name="lpFileName">Name of the library</param>
		/// <returns>A handle to the library</returns>
		[DllImport("kernel32.dll")]
		static extern IntPtr LoadLibrary(string lpFileName);
		#endregion
	}
}
