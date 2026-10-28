arm-none-eabi-as -march=armv4t -mcpu=arm7tdmi -o ScuffedBios.o ScuffedBios.s

arm-none-eabi-objcopy -O binary ScuffedBios.o ScuffedBios.bin

xxd -i ScuffedBios.bin
