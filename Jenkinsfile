pipeline {
    options {
        buildDiscarder(logRotator(numToKeepStr: '10', artifactNumToKeepStr: '5'))
    }
    agent {
        label 'windoc'
    }
    stages {
        stage('Clean') {
            steps {
                script {
                    bat "nuget restore"
                    bat "msbuild TusClient.proj /t:Clean"
                }
            }
        }
        stage('compile & build locally') {
            steps {
                script {
                    bat "msbuild TusClient.proj /t:Build"
                }
            }
        }
        stage('Publish') {
            steps {
                script {
                    bat "msbuild TusClient.proj /t:Publish"
                }
            }
        }
    }
}
