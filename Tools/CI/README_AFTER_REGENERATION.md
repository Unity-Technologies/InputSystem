After you regenerate the yamato jobs you need to make the following edit:

In .yamato\wrench\validation-jobs.yml find "validate_-_inputsystem_-_2019_4_-_ubuntu"-job and change
"image: package-ci/ubuntu-20.04:default" to "image: package-ci/ubuntu-18.04:v4"    
