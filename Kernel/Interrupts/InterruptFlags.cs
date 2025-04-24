namespace Kernel.Interrupts;

[System.Flags]
public enum InterruptFlags : byte
{
	Interrupt = 0b_0000_1110,
	Trap      = 0b_0000_1111,
	Present   = 0b_1000_0000,
	RING0     = 0b_0000_0000,
	RING1     = 0b_0010_0000,
	RING2     = 0b_0100_0000,
	RING3     = 0b_0110_0000,
}
