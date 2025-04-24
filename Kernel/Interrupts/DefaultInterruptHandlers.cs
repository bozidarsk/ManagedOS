using System;
using System.Runtime;

using Kernel.IO;

namespace Kernel.Interrupts;

#pragma warning disable CS8500

public static class DefaultInterruptHandlers 
{
	[Import] private static extern void loadpanic(ulong rdi, ulong rsi, ulong rbp, ulong rsp);

	public static void ThrowException(Registers registers, ulong error) 
	{
		Console.WriteLine("Exception '#BP Breakpoint' occured.");
		Console.WriteLine(registers);
		loadpanic(registers.rdi, registers.rsi, registers.rbp, registers.rsp);
	}

	public static void PageFault(Registers registers, ulong error) 
	{
		Console.WriteLine("Exception '#PF Page Fault' occured.");
		Console.WriteLine(registers);
		Environment.FailFast(
			"Unhandled exception.",
			(error == 0) ? new NullReferenceException() : new StackOverflowException()
		);
	}

	public static void Exception(int index, Registers registers, ulong error) 
	{
		string[] names = 
		{
			"#DE Divide Error",
			"#DB Debug",
			"Non-maskable external interrupt.",
			"#BP Breakpoint",
			"#OF Overflow",
			"#BR BOUND Range Exceeded",
			"#UD Invalid Opcode (Undefined Opcode)",
			"#NM Device Not Avaliable (No Math CoProcessor)",
			"#DF Double Fault",
			"#MF CoProcessor Segment Overrun",
			"#TS Invalid TSS",
			"#NP Segment Not Present",
			"#SS Stack Segment Fault",
			"#GP General Protection",
			"#PF Page Fault",
			"Reserved 15",
			"#MF Floating-Point Error (Math Fault)",
			"#AC Alignment Check",
			"#MC Machine Check",
			"#XM SIMD Floating-Point",
			"#VE Virtualization",
			"#CP Control Protection",
		};

		Console.WriteLine($"Exception '{names[index]}' occured.");
		Console.WriteLine(registers);
	}

	public static void InterruptRequest(int index, Registers registers, ulong error) 
	{
		string[] names = 
		{
			"PIT",
			"Keyboard",
			"8259A slave controller",
			"COM2 / COM4",
			"COM1 / COM3",
			"LPT2",
			"Floppy controller",
			"LPT1",
			"RTC",
			"Unassigned 9",
			"Unassigned 10",
			"Unassigned 11",
			"Mouse controller",
			"Math coprocessor",
			"Hard disk controller 1",
			"Hard disk controller 2",
		};

		int irq = index - PIC.Offset;
		Console.WriteLine($"Interrupt request '{names[irq]}' occured.");
	}

	public static void Unused(Registers registers, ulong error) 
	{
		Console.WriteLine("Unused interrupt occured.");
		Console.WriteLine(registers);
	}
}
