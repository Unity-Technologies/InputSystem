for Sample in ExternalSampleProjects/*/
do
	echo Running ${Method} on ${Sample}
	mkdir -p ${Sample}Assets/ExternalSamplesUtility/Editor
	ln -s ../../Packages ${Sample}Packages
	cp ExternalSampleProjects/ExternalSamplesUtility.cs ${Sample}Assets/ExternalSamplesUtility/Editor/
	${Editor} -batchmode -projectPath $Sample -executeMethod ExternalSamplesUtility.${Method}
	rm ${Sample}Assets/ExternalSamplesUtility/Editor/ExternalSamplesUtility.cs
	rm ${Sample}Packages
done