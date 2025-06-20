global _start64
global getkernelend
global stop

extern kmain
extern KERNELEND
extern MODULES
extern InitializeModules

section .text

; void stop()
stop:
	cli
	hlt

; nint getkernelend()
getkernelend:
	mov rax, KERNELEND
	ret

; rdi contains saved multiboot info address
_start64:
	mov ax, 0
	mov ss, ax
	mov ds, ax
	mov es, ax
	mov fs, ax
	mov gs, ax

	mov rax, cr0
	or rax, 1 << 1
	or rax, 1 << 2
	xor rax, 1 << 2
	mov cr0, rax

	mov rax, cr4
	or rax, 1 << 9
	or rax, 1 << 10
	mov cr4, rax

	; save bootinfo and align stack
	push rdi
	sub rsp, 8

	xor rdi, rdi
	mov rsi, MODULES
	mov rdx, 2
	xor rcx, rcx
	xor r8, r8
	xor rbp, rbp
	call InitializeModules

	xor rbp, rbp
	add rsp, 8
	pop rdi
	call kmain

	; hlt breaks interrupts
	jmp $
