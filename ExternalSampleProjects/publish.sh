for Sample in ExternalSampleProjects/*/
do
	echo Running ${Method} on ${Sample}
	mkdir -p ${Sample}Assets/ExternalSamplesUtility/Editor
	ln -s ../../Packages ${Sample}Packages
	cp ExternalSampleProjects/ExternalSamplesUtility.cs ${Sample}Assets/ExternalSamplesUtility/Editor/
	${Editor} -batchmode -projectPath $Sample -executeMethod ExternalSamplesUtility.${Method} -logFile upm-ci~/${Sample}Editor.log
	status=$?
	echo Editor returned $status
	rm ${Sample}Assets/ExternalSamplesUtility/Editor/ExternalSamplesUtility.cs
	rm ${Sample}Packages
	if [ $? -eq $status ]; then
		echo Ok
	else
		exit 1;
	fi
done