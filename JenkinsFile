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
                    bat "msbuild /t:clean /p:Configuration=Release"
                }
            }
        }
        stage('Compile & build locally') {
            steps {
                script {
                    bat "msbuild /t:build /p:Configuration=Release"
                }
            }
        }
        stage('Publish') {
            steps {
                script {
                    bat "msbuild /t:Publish /p:Config=Release tusclient.proj"
                }
            }
        }
    }
}
