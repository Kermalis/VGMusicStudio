
PA_PATH=$(PORTAUDIO_PATH)

portaudio-sharp.dll: $(CXXI_DLL) *.cs
	mcs -debug -t:library -out:portaudio-sharp.dll *.cs -unsafe

output/Libs.cs: portaudio.xml
	generator portaudio.xml --lib=portaudio --ns=Commons.Media.PortAudio

portaudio.xml: $(PA_PATH)/include/portaudio.h
	gccxml $(PA_PATH)/include/portaudio.h -fxml=portaudio.xml

clean:
	rm -rf portaudio-sharp.dll portaudio-sharp.dll.mdb
	rm -rf portaudio.xml
	rm -rf output/*.cs
