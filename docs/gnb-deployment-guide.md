## Deployment Steps:
0. The below steps will only be required in case we want to make any change in dotnet code/templates.
1. Checkout the FHIR-Converter code from - svn/github into local machine/EC2 container for build.
2. Docker should be installed on the system to create the image. The internet access would be required to download the dotnet aspnet sdk and other relevant dependencies.
3. After checkout, open the terminal and use below command to create the image file.
4. The image file needs to be uploaded to the AWS - ECR and run the corresponding task.

### Docker command to build:
docker build -t fhir_converter:v1 .

### AWS command to upload the image to ECR:

aws ecr get-login-password --region ca-central-1 | docker login --username AWS --password-stdin 767397816485.dkr.ecr.ca-central-1.amazonaws.com

docker tag fhir_converter:latest 767397816485.dkr.ecr.ca-central-1.amazonaws.com/fhirconverter:latest
