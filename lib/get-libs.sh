echo "Getting library files..."
echo "  Dir: $PWD"

bash install-package-from-libs-repository.sh CompulsiveCoder NUnit 2.6.4 || exit 1
bash install-package-from-libs-repository.sh CompulsiveCoder NUnit.Runners 2.6.4 || exit 1
bash install-package-from-libs-repository.sh CompulsiveCoder Newtonsoft.Json 11.0.2 || exit 1
bash install-package-from-libs-repository.sh CompulsiveCoder M2Mqtt 4.3.0.0 || exit 1
bash install-package-from-libs-repository.sh CompulsiveCoder M2Mqtt 4.3.0.0 || exit 1

bash install-package-from-github-release.sh CompulsiveCoder duinocom.core 1.2.0.27 || exit 1

echo "Finished getting library files."
