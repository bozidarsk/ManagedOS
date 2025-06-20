ENTRY(_start32)

PHDRS 
{
   multiboot PT_LOAD FLAGS (4);
   stack PT_LOAD FLAGS (6);
   framebuffer PT_LOAD FLAGS (6);
   text PT_LOAD FLAGS (5);
   rodata PT_LOAD FLAGS (4);
   data PT_LOAD FLAGS (6);
}

SECTIONS 
{
	.multiboot ALIGN(8) : 
	{
		*(.multiboot)
	} :multiboot

	. = 512 * 4K; /* first page is not present (page fault triggers before NullReferenceException and StackOverflowException) */

	.stack ALIGN(16) : 
	{
		KEEP(*(.stack))
	} :stack
	. += 3 * 2M;
	PROVIDE(STACKTOP = .); /* location must be less than uint.MaxValue (mov esp, STACKTOP), see Boot/boot.asm */

	PROVIDE(CONSOLEBUFFER = .);
	.framebuffer : 
	{
		KEEP(*(.framebuffer))
	} :framebuffer
	. += 3 * 2M;

	.text : 
	{
		*(.text)
		*(__managedcode)
	} :text

	.rodata : 
	{
		*(.rodata)
		*(.rdata)
	} :rodata

	.data : 
	{
		*(.data)
		*(__unbox)
		*(.dotnet_eh_table)
		*(.eh_frame)

		PROVIDE(MODULES = .);
		*(__modules)
	} :data

	.bss : 
	{
		*(.bss)
	} :data

	PROVIDE(KERNELEND = .);
}
