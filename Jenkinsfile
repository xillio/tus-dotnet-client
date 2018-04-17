pipeline {
    options {
        buildDiscarder(logRotator(numToKeepStr: '10', artifactNumToKeepStr: '5'))
    }
    agent {
        label 'windoc'
    }
    stages {
        stage('Initialize environment') {
            steps {
                script {
                    echo "Determining configuration from branch name: ${env.BRANCH_NAME}"
                    configuration = env.BRANCH_NAME == "master" ? "Release" : "Debug"
                    echo "Using configuration ${configuration}"
                }
            }
        }
        stage('Clean') {
            steps {
                script {
                    bat "nuget restore"
                    bat "msbuild TusClient.proj /p:Config=${configuration} /t:Clean"
                }
            }
        }
        stage('compile & build locally') {
            steps {
                script {
                    bat "msbuild TusClient.proj /p:Config=${configuration} /t:Build"
                }
            }
        }
        stage('Publish') {
            steps {
                script {
                    bat "msbuild TusClient.proj /p:Config=${configuration} /t:Publish"
                }
            }
        }
    }
}
